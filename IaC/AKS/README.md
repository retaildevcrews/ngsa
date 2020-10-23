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
export He_Name=[your unique name]

### if true, change He_Name
az cosmosdb check-name-exists -n ${He_Name}

```

#### Create Resource Groups

> When experimenting with this sample, you should create new resource groups to avoid accidentally deleting resources
>
> If you use an existing resource group, please make sure to apply resource locks to avoid accidentally deleting resources

- You will create 2 resource groups
  - One for AKS, and Azure Monitor
  - One for Cosmos DB

```bash

# set location
export He_Location=westus2

# set the subscription
export He_Sub='az account show --query id -o tsv'

# resource group names
export Imdb_Name=$He_Name
export He_App_RG=${He_Name}-rg-app
export Imdb_RG=${Imdb_Name}-rg-cosmos

# export Cosmos DB env vars
# these will be explained in the Cosmos DB setup step
export Imdb_Location=$He_Location
export Imdb_DB=imdb
export Imdb_Col=movies
export Imdb_RW_Key='az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv'

# create the resource groups
az group create -n $He_App_RG -l $He_Location
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

#### Create Azure Monitor

> The Application Insights extension is in preview and needs to be added to the CLI

```bash

# Add App Insights extension
az extension add -n application-insights
az feature register --name AIWorkspacePreview --namespace microsoft.insights
az provider register -n microsoft.insights

# Create App Insights
az monitor app-insights component create -g $He_App_RG -l $He_Location -a $He_Name -o table

```

#### Create the AKS Cluster

Set local variables to use in AKS deployment

```bash

export He_AKS_Name="${He_Name}-aks"

```

Determine the latest version of Kubernetes supported by AKS. It is recommended to choose the latest version not in preview for production purposes, otherwise choose the latest in the list.

```bash

az aks get-versions -l $He_Location -o table

export He_K8S_VER=1.18.8

```

Create and connect to the AKS cluster.

```bash

# note: if you see the following failure, navigate to your .azure\ directory
# and delete the file "aksServicePrincipal.json":
#    Waiting for AAD role to propagate[################################    ]  90.0000%Could not create a
#    role assignment for ACR. Are you an Owner on this subscription?

# this step usually takes 2-4 minutes
az aks create --name $He_AKS_Name --resource-group $He_App_RG --location $He_Location --enable-cluster-autoscaler --min-count 3 --max-count 6 --node-count 3 --kubernetes-version $He_K8S_VER --no-ssh-key

az aks get-credentials -n $He_AKS_Name -g $He_App_RG

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
kubectl create ns istio-system
kubectl apply -f $REPO_ROOT/IaC/AKS/cluster/manifests/istio/istio.aks.yaml

# the istio resources will take about a minute to be installed

```

Validate the Istio installation

```bash

kubectl get all -n istio-system

```

You should see the following components:

- `istio*` - the Istio components
- `jaeger-*`, `tracing`, and `zipkin` - tracing addon
- `prometheus` - metrics addon
- `grafana` - analytics and monitoring dashboard addon
- `kiali` - service mesh dashboard addon

Enable automatic sidecar injection in the default namespace:

```bash

kubectl label namespace default istio-injection=enabled

```

Get the public IP of the Istio Ingress Gateway and set the application endpoint.

```bash

export INGRESS_PIP=$(kubectl --namespace istio-system  get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')
export He_App_Endpoint=http://${INGRESS_PIP}.nip.io

```

## Install KEDA

KEDA autoscales the NGSA pods by assessing metrics for incoming requests, which are captured by Istio and stored in Prometheus.

```bash

kubectl create ns keda
helm install keda kedacore/keda -n keda

```

## Deploy NGSA Secrets

```bash

kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
  --from-literal=AppInsightsKey=$(az monitor app-insights component show -g $He_App_RG -a $He_Name --query instrumentationKey -o tsv)

```



## Deploy NGSA with Helm

An helm chart is included for the reference application ([NGSA](https://github.com/retaildevcrews/ngsa))

Install the Helm Chart located in the cloned directory

```bash

cd $REPO_ROOT/IaC/AKS/cluster/charts

```

```bash
# Before installing the chart, ensure <INGRESS_PIP> is updated in your Values.yaml file with the value of $INGRESS_PIP

# Install NGSA using the upstream ngsa image from Dockerhub
helm install ngsa-aks ngsa/

# the application generally takes about 2-4 minutes to be ready

# check the version endpoint
# you may get a timeout error, if so, just retry

http ${He_App_Endpoint}/version

```

Run the Validation Test

> For more information on the validation test tool, see [Web Validate](https://github.com/retaildevcrews/webvalidate)


## Observability

TODO


## Smoke Test

TODO
