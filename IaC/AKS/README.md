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
- Istioctl ([Install Instructions](https://istio.io/latest/docs/setup/getting-started/#download))

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

This walkthrough will create resource groups, a Cosmos DB instance, Azure DNS entry(if specified) and an Azure Kubernetes Service (AKS) cluster. An automation script is available which can be used instead of this walkthrough. Script usage instruction is found [here](#aks-cluster-using-automated-script)

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
- Follow the steps in the [IMDb Repo](https://github.com/retaildevcrews/imdb#create-cosmos-db-server-database-and-container) to create a Cosmos DB server, database, and collection and load the sample IMDb data
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

# create Log Analytics for the loderunner clients
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

export Ngsa_K8S_VER=1.19.6

```

Create and connect to the AKS cluster.

```bash

# this step usually takes 2-4 minutes
az aks create --name $Ngsa_AKS_Name --resource-group $Ngsa_App_RG --location $Ngsa_Location --enable-cluster-autoscaler --min-count 3 --max-count 6 --node-count 3 --kubernetes-version $Ngsa_K8S_VER --no-ssh-key

# note: if you see the following failure, navigate to your .azure\ directory
# and delete the file "aksServicePrincipal.json":
#    Waiting for AAD role to propagate[################################    ]  90.0000%Could not create a
#    role assignment for ACR. Are you an Owner on this subscription?

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

helm repo add stable https://charts.helm.sh/stable
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

# Check that istio has an ingress IP. If not, wait a few seconds and run the above command again.
echo $INGRESS_PIP

# set application endpoint
export Ngsa_App_Endpoint="${INGRESS_PIP}.nip.io"

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

kubectl create secret generic ngsa-secrets \
  --namespace ngsa \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/

```

## Deploy NGSA with Helm

The NGSA application has been packed into a Helm chart for deployment into the cluster. The following instructions will walk you through the manual process of deployment of the helm chart and is recommended for development and testing. Alternatively, the helm chart can be deployed in a GitOps CICD approach. GitOps allows the automated deployment of the application to the cluster using FluxCD in which the configuration of the application is stored in Git.([NGSA-CD](https://github.com/retaildevcrews/ngsa-cd)).

```bash

cd $HOME

git clone git@github.com:retaildevcrews/ngsa-cd.git

export CHART_REPO=$HOME/ngsa-cd

cd $CHART_REPO/charts/ngsa

# Use the helm-config.yaml file to configure the deployment
envsubst < helm-config.example.yaml > helm-config.yaml

```

The `helm-config.yaml` file can be used as an override to the default values during the helm install.

```bash

cd $CHART_REPO/charts/

# create an istio gateway

cat <<EOF | kubectl apply -n ngsa -f -
apiVersion: networking.istio.io/v1beta1
kind: Gateway
metadata:
  name: ngsa-gateway
spec:
  selector:
    istio: ingressgateway
  servers:
  - port:
      number: 80
      name: http
      protocol: HTTP
    hosts:
      - "${Ngsa_App_Endpoint}"
EOF

# Install NGSA using the upstream ngsa image from Dockerhub
helm install ngsa-aks ngsa -f ./ngsa/helm-config.yaml --namespace ngsa --set cert.enabled=false --set gateway.name=ngsa-gateway
# Note: Above command creates a ngsa cosmos deployment named ngsa-aks

# check the version endpoint
# you may get a timeout error, if so, just retry

http ${Ngsa_App_Endpoint}/version

```

Run the Validation Test

> For more information on the validation test tool, see [Lode Runner](../../src/Ngsa.LodeRunner).

```bash

# run the tests in a container
docker run -it --rm retaildevcrew/loderunner:beta --server "http://$Ngsa_App_Endpoint" --files baseline.json

```

## Smoke Tests

Deploy Loderunner to drive consistent traffic to the AKS cluster for monitoring.

```bash

cd $REPO_ROOT/IaC/AKS/cluster/charts

kubectl create namespace ngsa-l8r

cp ./loderunner/helm-config.example.yaml ./loderunner/helm-config.yaml

helm install l8r loderunner -f ./loderunner/helm-config.yaml --namespace ngsa-l8r

# Verify the pods are running
kubectl get pods --namespace ngsa-l8r

```

## Observability

Observability is enabled through a combination of Fluent Bit to forward logs to Azure Log Analytics and queries directly to Log Analytics or via Azure Dashboards.

### Fluent Bit Log Forwarding

Deploy Fluent Bit to forward application and smoker logs to the Log Analytics instance.

```bash

cd $CHART_REPO/charts/

kubectl create namespace fluentbit

kubectl create secret generic fluentbit-secrets \
  --namespace fluentbit \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv)

helm install fluentbit fluentbit --namespace fluentbit --set log.region=DEBUG --set log.zone=DEBUG

# Verify the fluentbit pod is running
kubectl get pod --namespace fluentbit

```

### Querying Log Analytics

Navigate to the Log Analytics resource in the Azure portal and go to General -> Logs to explore the logs with KQL queries.

Sample queries:

```bash

# View the latest logs from the data service

ngsa_CL
| where k_container_s == "ds"
   and LogName_s == "Ngsa.RequestLog"
| project TimeGenerated, CosmosName_s, Zone_s, CVector_s, Duration_d, StatusCode_d, Path_s

# Calculate the 75th and 95th percentiles for the ngsa app response time and compare by app type (in-memory or cosmos) and zone

ngsa_CL
| where k_container_s == "app"
   and k_app_s == "ngsa-aks"
   and LogName_s == "Ngsa.RequestLog"
| summarize percentile(Duration_d, 75), percentile(Duration_d, 95) by Zone_s, k_app_s
| extend Zone=Zone_s, 75th=round(percentile_Duration_d_75,2), 95th=round(percentile_Duration_d_95,2), AppType=k_app_s
| project AppType, Zone, 75th, 95th
| order by AppType, Zone asc

```

## AKS Cluster using automated script

With this script a cluster can be deployed in AKS (uses the same steps above).
The script is self-contained, meaning, it won't change the user-environment (e.g. selected Azure Subscription or ubernetes context) unless it's explicitly specified.
It is located [here](./scripts/create-cluster-env.bash).
Script Usage:

```bash
    ./create-cluster-env.bash --ngsa-prefix basename123 [Optional Args/Flags]
    ./create-cluster-env.bash -s azure-subs -n basename123 [Optional Args/Flags]

Required args:
    -n | --ngsa-prefix BASE_NAME    This will be the NGSA prefix for all resources
                                    Do not include punctuation - only use a-z and 0-9
                                    must be at least 5 characters long
                                    must start with a-z (only lowercase)
Optional args:
    -s | --subscription AZ_SUB      Azure Subscription Name or ID
    -e | --env ENVIRONMENT          Environemnt Type. Default: dev (See README.md for other values)
    -d | --domain DOMAIN_NAME       Registered Domain Name. Default: nip.io (Requires --email)
    -m | --email EMAIL_DOMAIN       Required Email if a '--domain' is given
    -l | --location LOCATION        Location where the resources will be created. Default: westus2
    -k | --k8s-ver K8S_VERSION      Kubernetes version used. Default: 1.18.8
                                    Use 'az aks get-versions -l westus2 -o table' to get supported versions
    -c | --node-count NODE_COUNT    Cluster Node Count. Default: 3
    -r | --dns-rg DNS_RG            DNS Resource group name. Default: dns-rg
    -i | --cosmos-key COSMOS_KEY
    -u | --cosmos-url COSMOS_URL    In case users want to use their own CosmosDBBoth Key and URL are empty by default.
Optional Flag:
    -x | --set-k8s-context          Sets the kubernetes context for current user in /home/kushal/.kube/config
    -h | --help                     Show the usage
```

Example usage:

- Create a cluster with selected Azure subscription

  `./create-cluster-env.bash --ngsa-prefix basengsa`
- Create a cluster with specific Azure subscription

  `./create-cluster-env.bash -s azure-subscription-name -n basengsa`
- Create a cluster in a specific location

  `./create-cluster-env.bash -n basengsa -l centralus`
- Create a cluster and set the current k8s context

  `./create-cluster-env.bash --subscription "az-sub" -n basengsa --set-k8s-context`
- Create a cluster with specific environmen type

  `./create-cluster-env.bash --subscription "az-sub" -n ultrangsa -d abcd.efg --email user@email.org --env stage`
- Create a cluster with specific domain name

  `./create-cluster-env.bash --subscription "az-sub" -n basengsa -d abcd.efg --email user@email.org`
- Create a cluster with existing CosmosDB

  `./create-cluster-env.bash -s az-sub -n ngsatest -d abcd.ms -l centralus -i AkI=FAKE=KEY=oGk=SOME=FAKE=KEY=Zh7Iad703gWwBb0P=YET=ANOTHER=FAKE=KEY=w0Zubg== -u https://sample-cosmos-db.documents.zure.com:443/`
- Create a cluster with specific node count

  `./create-cluster-env.bash --subscription "az-sub" -n basengsa -c 6 -x`
