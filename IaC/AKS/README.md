# Deployment Walk Through

## Background

The following instructions allow the deployment of NGSA application in AKS with Istio and Keda.

### Azure Components in Use

- Azure Kubernetes Service
  - Istio ServiceMesh
  - Keda
  - Prometheus
  - Kiali
  - Grafana
- Azure Cosmos DB
- Application Insights
- DNS Zone

### Prerequisites

- Azure subscription with permissions to create:
  - Resource Groups, Service Principals, Cosmos DB, AKS, Azure Monitor
- Bash shell (tested on Mac, Ubuntu, Windows with WSL2)
  - Will not work in Cloud Shell or WSL1
- Azure CLI ([download](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest))
- Docker CLI ([download](https://docs.docker.com/install/))
- Visual Studio Code (optional) ([download](https://code.visualstudio.com/download))
- kubectl (install by using `sudo az aks install-cli`)
- Helm v3 ([Install Instructions](https://helm.sh/docs/intro/install/))

### DNS, SSL/TLS Prerequisites

 A domain name and SSL/TLS certificates are required for HTTPS access over the internet.

- Registered domain with permissions to update nameservers
- Azure subscription with permissions to create a DNS Zone

### Setup

Fork this repo and clone to your local machine

```bash

cd $HOME

git clone https://github.com/retaildevcrews/ngsa

```

Change into the base directory of the repo

```bash

cd ngsa

export REPO_ROOT=$(pwd)

```

#### Login to Azure and select subscription

```bash

az login

# show your Azure accounts
az account list -o table

# select the Azure account
az account set -s {subscription name or Id}

```

This walkthrough will create resource groups, a Cosmos DB instance, and an Azure Kubernetes Service (AKS) cluster.

#### Choose a unique DNS name

```bash

# this will be the prefix for all resources
# do not include punctuation - only use a-z and 0-9
# must be at least 5 characters long
# must start with a-z (only lowercase)
export Ngsa_Base_Name=[your unique name]
# examples: pre, test, stage, prod, and dev
export Ngsa_Env=[your environment name]

# Set main resource name
export Ngsa_Name="${Ngsa_Base_Name}-${Ngsa_Env}"

# Set email to register with Let's Encrypt
export Ngsa_Email=[your email address]

# Set your registered domain name.
# example: export Ngsa_Domain_Name=cse.ms
export Ngsa_Domain_Name=[your domain name]

### if true, change Ngsa_Name
az cosmosdb check-name-exists -n "${Ngsa_Name}-cosmos"

```

#### Create Resource Groups

> When experimenting with this sample, you should create new resource groups to avoid accidentally deleting resources
>
> If you use an existing resource group, please make sure to apply resource locks to avoid accidentally deleting resources

- You will create 3 resource groups
  - One for AKS and Azure Monitor
  - One for Cosmos DB
  - One for Log Analytics

```bash

# set location
export Ngsa_Location=westus2

# set application endpoint
export Ngsa_App_Endpoint="${Ngsa_Name}.${Ngsa_Domain_Name}"

# resource group names
export Imdb_Name="${Ngsa_Name}-cosmos"
export Ngsa_App_RG="${Ngsa_Name}-app-rg"
export Ngsa_Log_Analytics_RG="${Ngsa_Name}-log-rg"
export Imdb_RG="${Ngsa_Name}-cosmos-rg"

# export Cosmos DB env vars
# these will be explained in the Cosmos DB setup step
export Imdb_Location=$Ngsa_Location
export Imdb_DB=imdb
export Imdb_Col=movies
export Imdb_RW_Key='az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv'

# create the resource groups
az group create -n $Ngsa_App_RG -l $Ngsa_Location
az group create -n $Ngsa_Log_Analytics_RG -l $Ngsa_Location
az group create -n $Imdb_RG -l $Imdb_Location

```

#### Create and load sample data into Cosmos DB

- This takes several minutes to run
- This reference app is designed to use a simple dataset from IMDb of 1300 movies and their associated actors and genres
- Follow the steps in the [IMDb Repo](https://github.com/retaildevcrews/imdb) to create a Cosmos DB server, database, and collection and load the sample IMDb data
  - The repo readme also provides an explanation of the data model design decisions

  > You can safely start with the Create Cosmos DB step
  >
  > The initial steps were completed above

#### Create Log Analytics

Add log analytics extension.

```bash

export Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"

# Add Log Analytics extension
az extension add -n log-analytics

# create Log Analytics for the webv clients
az monitor log-analytics workspace create -g $Ngsa_Log_Analytics_RG -l $Ngsa_Location -n $Ngsa_Log_Analytics_Name -o table

```

#### Create the AKS Cluster

Set local variables to use in AKS deployment

```bash

export Ngsa_AKS_Name="${Ngsa_Name}-aks"

```

Determine the latest version of Kubernetes supported by AKS. It is recommended to choose the latest version not in preview for production purposes, otherwise choose the latest in the list.

```bash

az aks get-versions -l $Ngsa_Location -o table

export Ngsa_K8S_VER=1.18.8

```

Create and connect to the AKS cluster.

```bash

# note: if you see the following failure, navigate to your .azure\ directory
# and delete the file "aksServicePrincipal.json":
#    Waiting for AAD role to propagate[################################    ]  90.0000%Could not create a
#    role assignment for ACR. Are you an Owner on this subscription?

# this step usually takes 2-4 minutes
az aks create --name $Ngsa_AKS_Name --resource-group $Ngsa_App_RG --location $Ngsa_Location --enable-cluster-autoscaler --min-count 3 --max-count 6 --node-count 3 --kubernetes-version $Ngsa_K8S_VER --no-ssh-key

az aks get-credentials -n $Ngsa_AKS_Name -g $Ngsa_App_RG

kubectl get nodes

```

## Install Helm 3

Install the latest version of Helm by download the latest [release](https://github.com/helm/helm/releases):

```bash

# mac os
OS=darwin-amd64 && \
REL=v3.3.4 && \ #Should be lastest release from https://github.com/helm/helm/releases
mkdir -p $HOME/.helm/bin && \
curl -sSL "https://get.helm.sh/helm-${REL}-${OS}.tar.gz" | tar xvz && \
chmod +x ${OS}/helm && mv ${OS}/helm $HOME/.helm/bin/helm
rm -R ${OS}

```

or

```bash

# Linux/WSL
OS=linux-amd64 && \
REL=v3.3.4 && \
mkdir -p $HOME/.helm/bin && \
curl -sSL "https://get.helm.sh/helm-${REL}-${OS}.tar.gz" | tar xvz && \
chmod +x ${OS}/helm && mv ${OS}/helm $HOME/.helm/bin/helm
rm -R ${OS}

```

Add the helm binary to your path and set Helm home:

```bash

export PATH=$PATH:$HOME/.helm/bin
export HELM_HOME=$HOME/.helm

```

>NOTE: This will only set the helm command during the existing terminal session. Copy the 2 lines above to your bash or zsh profile so that the helm command can be run any time.

Verify the installation with:

```bash

helm version

```

Add the required helm repositories

```bash

helm repo add stable https://kubernetes-charts.storage.googleapis.com
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jetstack https://charts.jetstack.io
helm repo update

```

## Install Istio Service Mesh into the cluster

Specify the Istio version that will be leveraged throughout these instructions. Note: If using a macOS device, make sure to set ARCH to `osx`.

```bash

export ISTIO_VERSION=1.7.3
export ARCH=linux-amd64
curl -sL "https://github.com/istio/istio/releases/download/$ISTIO_VERSION/istioctl-$ISTIO_VERSION-$ARCH.tar.gz" | tar xz

```

Copy the istioctl client binary to the standard user program location in your PATH

```bash

sudo mv ./istioctl /usr/local/bin/istioctl
sudo chmod +x /usr/local/bin/istioctl

```

Install the Istio Operator and Components on AKS

```bash

istioctl operator init
kubectl create namespace istio-system
kubectl apply -f $REPO_ROOT/IaC/AKS/cluster/manifests/istio/istio.aks.yaml

# the istio resources will take about a minute to be installed

```

Validate the Istio installation

```bash

kubectl get all --namespace istio-system

```

You should see the following components:

- `istio*` - the Istio components
- `jaeger-*`, `tracing`, and `zipkin` - tracing addon
- `prometheus` - metrics addon
- `grafana` - analytics and monitoring dashboard addon
- `kiali` - service mesh dashboard addon

Enable automatic sidecar injection in the ngsa namespace:

```bash

kubectl create namespace ngsa
kubectl label namespace ngsa istio-injection=enabled

```

Get the public IP of the Istio Ingress Gateway.

```bash

export INGRESS_PIP=$(kubectl --namespace istio-system  get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')

```

## Install KEDA

KEDA autoscales the NGSA pods by assessing metrics for incoming requests, which are captured by Istio and stored in Prometheus.

```bash

export KEDA_VERSION=2.0.0
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda --version $KEDA_VERSION

```

## Deploy NGSA Secrets

```bash

kubectl create secret generic ngsa-aks-secrets \
  --namespace ngsa \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv)

```

## Setup SSL/DNS

> Note: A registered domain name is required for this section.

### DNS Setup

Create a DNS A record mapping your domain to the Istio ingress gateway IP address.

This is a setup using Azure DNS. In this setup, update your domain to use Azure DNS Zone nameservers.

```bash

# example: export Ngsa_DNS_RG=dns-rg
export Ngsa_DNS_RG=[dns resource group name]

# Check if DNS resource group exists
az group exists -n $Ngsa_DNS_RG

# If false, create DNS resource group
az group create -n $Ngsa_DNS_RG -l $Ngsa_Location

# Check if DNS Zone exists
az network dns zone show --name $Ngsa_Domain_Name -g $Ngsa_DNS_RG -o table

# If not found, create the DNS Zone.
az network dns zone create -g $Ngsa_DNS_RG -n $Ngsa_Domain_Name

# Add DNS A record for the Istio ingress gateway.
az network dns record-set a add-record -g $Ngsa_DNS_RG -z $Ngsa_Domain_Name -n $Ngsa_Name -a $INGRESS_PIP

# Show the Azure nameservers for your DNS Zone.
az network dns zone show -n $Ngsa_Domain_Name -g $Ngsa_DNS_RG --query nameServers -o tsv

# Update your domain to use the result entries for nameservers.

```

### Install Cert-Manager

```bash

cd $REPO_ROOT/IaC/AKS/cluster/manifests/cert-manager

export CERT_MANAGER_VERSION=1.0.3
kubectl create namespace cert-manager
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --version "v${CERT_MANAGER_VERSION}" \
  --set installCRDs=true

# wait for the cert manager pods to be ready
kubectl get pods --namespace cert-manager

# Create a staging and production ClusterIssuer for cert-manager
# Use the staging ClusterIssuer for testing. Once ready, use the production resource.
envsubst < clusterissuer.yaml | kubectl apply -f -

```

## Deploy NGSA with Helm

A helm chart is included for the reference application ([NGSA](https://github.com/retaildevcrews/ngsa)).

```bash

cd $REPO_ROOT/IaC/AKS/cluster/charts/ngsa

# Use the helm-config.yaml file to configure the deployment
envsubst < helm-config.example.yaml > helm-config.yaml

```

The `helm-config.yaml` file can be used as an override to the default values during the helm install.

```bash

cd $REPO_ROOT/IaC/AKS/cluster/charts/

# Install NGSA using the upstream ngsa image from Dockerhub
# Start by using the "letsencrypt-staging" ClusterIssuer to get test certs from the Let's Encrypt staging environment.
helm install ngsa-aks ngsa -f ./ngsa/helm-config.yaml --namespace ngsa --set cert.issuer=letsencrypt-staging

# check the version endpoint
# you may get a timeout error, if so, just retry

http ${Ngsa_App_Endpoint}/version

```

Check that the test certificates have been issued. You can check in the browser, or use curl. With the test certificates, it is expected that you get a privacy error.

```bash

export Ngsa_Https_App_Endpoint="https://${Ngsa_App_Endpoint}"

# Curl the https endpoint. You should see a certificate problem. This is expected with the staging certificates from Let's Encrypt.
curl $Ngsa_Https_App_Endpoint

```

After verifying that the test certs were issued, update the deployment to use the "letsencrypt-prod" ClusterIssuer to get valid certs from the Let's Encrypt production environment.

```bash

helm upgrade ngsa-aks ngsa -f ./ngsa/helm-config.yaml  --namespace ngsa --set cert.issuer=letsencrypt-prod

```

Run the Validation Test

> For more information on the validation test tool, see [Web Validate](https://github.com/retaildevcrews/webvalidate)

```bash

# run the tests in a container
docker run -it --rm retaildevcrew/webvalidate --server $Ngsa_Https_App_Endpoint --base-url https://raw.githubusercontent.com/retaildevcrews/ngsa/main/TestFiles/ --files baseline.json

```

## Observability

TODO

## Smoke Tests

Deploy Web Validate to drive consistent traffic to the AKS cluster for monitoring.

```bash

cd $REPO_ROOT/IaC/AKS/cluster/charts

kubectl create namespace ngsa-smoker
kubectl create secret generic ngsa-smoker-secrets \
  --namespace ngsa-smoker \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv)

helm install ngsa-smoker smoker --namespace ngsa-smoker --set ingressURL=$Ngsa_Https_App_Endpoint

# Verify the pods are running
kubectl get pods --namespace ngsa-smoker

```
