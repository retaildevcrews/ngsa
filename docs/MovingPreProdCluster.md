# Walk Through: Moving Pre Production Environment to New Cluster

## Background

The following instructions to move current pre production AKS cluster to a new cluster.

Order of operations

- create aks clusters with availability zones for new environments - done
- creating new dns names for new environments
- create secrets (tls, ngsa, fluentbit etc) in new cluster
- install ngsa app in new environments (using fluxcd)
  - sync old cluster to a new branch in ngsa-cd (e.g. `old-preprod`)
  - install fluxcd in new cluster and sync new cluster to ngsa-cd repo with flux ci/cd
    - it will install ngsa app, fluentbit and loderunner in new environments
    - ngsa will be installed without cert-manager certificate.
    - lookout for potential issues with flux ci/cd. how to do this without breaking old environments?
  - test ngsa app in new environment with new dns names
- switch dns records to point IP address from old to new
  - test ngsa app in new environment with old dns names
- move shared resources. cosmos, log analytics
  - test ngsa app in new environment with old dns names
- update dashboard and alerts
- shutdown previous environments and resources

### Azure Components in Use

- Azure Kubernetes Service
- Azure Cosmos DB
- Azure Log Analytics
- DNS Zone

### Prerequisites

- Azure subscription with permissions to create:
  - Resource Groups, Service Principals, Cosmos DB, AKS, Azure Monitor
- Bash shell (tested on Mac, Ubuntu, Windows with WSL2)
  - Will not work in Cloud Shell or WSL1
- Azure CLI ([download](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest))
- Docker CLI ([download](https://docs.docker.com/install/))
- Visual Studio Code (optional) ([download](https://code.visualstudio.com/download))
- Kubectl (install by using `sudo az aks install-cli`)
- Helm v3 ([Installation Instructions](https://helm.sh/docs/intro/install/))
- Istioctl ([Installation Instructions](https://istio.io/latest/docs/setup/getting-started/#download))

This document is a quick guide to move the current Pre Prod environment to a new one.
For detailed guide to create a new environment and detailed warnings goto [IaC/AKS/README.md][IaC-Readme]

### DNS, SSL/TLS Prerequisites

 A domain name and SSL/TLS certificates are required for HTTPS access over the internet.

- Registered domain with permissions to update nameservers
- Azure subscription with permissions to create a DNS Zone

## Setup

### Login to Azure and select subscription

```bash

az login
# show your Azure accounts
az account list -o table
# select the Azure account
az account set -s {subscription name or Id}

```

### Repo Clone and variable setup

```bash

git clone https://github.com/retaildevcrews/ngsa
cd ngsa
export REPO_ROOT=$(pwd)

export Ngsa_Base_Name=ngsa

# examples: pre, test, stage, prod, and dev
export Ngsa_Env=pre

# Set main resource name
export Ngsa_Name="${Ngsa_Base_Name}-${Ngsa_Env}"

# Set email to register with Let's Encrypt
export Ngsa_Email=[your email address]

```

### Create AKS Cluster(s)

First create a single RG for AKS clusters in different zones: `az group create -n  $Ngsa_App_RG -l westus2`

#### Create AKS Cluster in CentralUS

```bash

# Create AKS cluster:
az aks create --name ngsa-pre-central-aks --resource-group $Ngsa_App_RG --location centralus --enable-cluster-autoscaler --min-count 3 --max-count 6 --node-count 3 --kubernetes-version 1.18.8 --no-ssh-key --zones 1 2 3

# Get K8s context
az aks get-credentials -n ngsa-pre-central-aks -g ngsa-pre-app-rg

# Update Helm repos

helm repo add stable https://charts.helm.sh/stable
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install Istio Service mesh
istioctl operator init
kubectl create namespace istio-system
kubectl apply -f $REPO_ROOT/IaC/AKS/cluster/manifests/istio/istio.aks.yaml

# Wait for some time for istio to deploy. Then export istio’s INGRESS public IP
export INGRESS_PIP=$(kubectl --namespace istio-system get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')

```

### Create Secrets in the New Cluster

```bash

# Enable Sidecar injection

kubectl create namespace ngsa
kubectl label namespace ngsa istio-injection=enabled
# Install KEDA
export KEDA_VERSION=2.0.0
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda --version $KEDA_VERSION

# Create NGSA and Fluentbit Secrets (Cosmos, Log Analytics etc)
kubectl create secret generic ngsa-secrets \
        --namespace ngsa \
        --from-literal=CosmosDatabase=imdb \
        --from-literal=CosmosCollection=movies \
        --from-literal=CosmosKey=$(az cosmosdb keys list -n ngsa-pre-west-cosmos -g ngsa-pre-west-cosmos-rg --query primaryReadonlyMasterKey -o tsv) \
        --from-literal=CosmosUrl=https://ngsa-pre-west-cosmos.documents.azure.com:443/

kubectl create namespace fluentbit
# Get Log analytics Workspace ID and Secret and save it as Secret for FluentBit
kubectl create secret generic fluentbit-secrets --namespace fluentbit --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g ngsa-pre-shared-rg -n ngsa-pre-west-log --query customerId -o tsv) --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g ngsa-pre-shared-rg -n ngsa-pre-west-log --query primarySharedKey -o tsv)

```

DNS Setup [W!ld CARD]

```bash

# Create a new DNS record newpre.cse.ms, and point that new record to new cluster
export INGRESS_PIP=$(kubectl --namespace istio-system  get svc -l istio=ingressgateway a-o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')

# Create a new DNS Record
az network dns record-set a add-record -g $Ngsa_DNS_RG -z cse.ms -n newpre -a $INGRESS_PIP

```

Install the new wildcard certificates (obtain the cse.ms wild-card certificate from teammates)

```bash

# First combine .crt and .ca-bundle file into one certificate
cat STAR_cse_ms.crt > STAR_cse_ms_merged.txt && cat STAR_cse_ms.ca-bundle >> STAR_cse_ms_merged.txt

# Then Create TLS secret from cert and key file
kubectl create secret tls ngsa-cert --key=cse_ms.key --cert=STAR_cse_ms_merged.txt -n istio-system

```

We need to make sure the secret name (ngsa-cert in this case) is the same in ngsa helm-config variable `cert.name`

### Install NGSA App

Clone the [ngsa-cd repo](https://github.com/retaildevcrews/ngsa-cd/). 

```bash

cd $HOME
export Git_Url=https://github.com/retaildevcrews/ngsa-cd
git clone $Git_Url
export NGSA_CD_REPO=$HOME/ngsa-cd
cd $NGSA_CD_REPO

```

Create helm-config.yaml and modify ngsa-cd helm chart (in values: ingress.hosts) to add the old host (pre.cse.ms)

#### Sync Old Cluster to a New Branch

Create a new branch and modify the HelmRelease files. We are modifying the branch name (`ref: main`) in each helm release yaml file.

```bash
export Git_Branch="old-preprod"
cd $NGSA_CD_REPO
# Create new branch
git checkout -b $Git_Branch

# Modify all Helm Release files for each zone
# In each yaml under `spec:`, there is a `ref: main`, we'll replace that with our branch name `ref: old-preprod`
cd releases/preprod-old/west/
sed -i "s/ref: main/ref: $Git_Branch/g" *.yaml

cd releases/preprod-old/east/
sed -i "s/ref: main/ref: $Git_Branch/g" *.yaml

cd releases/preprod-old/central/
sed -i "s/ref: main/ref: $Git_Branch/g" *.yaml

# Check modifications
git diff

# Add, commit and push
git add -u
git commit -m 'Message'
git push -u origin $Git_Branch

```

Now manually update the old pre-prod clusters for each zones (east, west and central) to point to new branch in fluxcd deployment

```bash

# For east
export Git_Path=releases/preprod/east

# Use the az aks or kubectl config command to set the current kube context to old-east
az aks get-credentials --resource-group ngsa-pre-east-app-rg --name ngsa-pre-east-aks --context east-old

# or if you already have the context setup:
kubectl config use-context east-old

# Make sure your env variables are properly set for the next steps
echo $Git_Url $Git_Branch $Git_Path

```

Then follow the [FluxCD helm instructions]([IAC](https://github.com/retaildevcrews/ngsa-cd/#installation-instructions)) to install and force update the flux cd.

#### Install FluxCD in New Cluster

Switch to the main branch for the new pre-prod environment.
----------------[UNFINISHED]-----------------------
```bash

cd $NGSA_CD_REPO
git checkout main

# Export proper variables for east
export Git_Url=https://github.com/retaildevcrews/ngsa-cd
export Git_Branch="old-preprod"
# For east
export Git_Path=releases/preprod/east

```

- To deploy the ngsa-app follow this guide [IaC-Readme-DeployNGSA] (it should be accepting traffic from `newpre.cse.ms`)
- Deploy l8r
- Check dashboard and log analytics output
- Switch IP address of old subdomain, pre.cse.ms, to new cluster.

### Move Cosmos to a new RG

We create a new RG and move the current CosmosDB instance to a new RG.

```bash

# Create a new shared resource group.
az group create -n ngsa-pre-shared-rg -l westus2
# Move CosmosDB instance “ngsa-pre-west-cosmos” from “ngsa-pre-west-cosmos-rg” to “ngsa-pre-shared-rg” using Azure Portal to the commands below.
# Query CosmosDB ID
az cosmosdb show -n ngsa-pre-west-cosmos -g ngsa-pre-west-cosmos-rg --query id

# Move CosmosDB
az resource move --destination-group ngsa-pre-shared-rg –ids {ID_OF_COSMOS_DB}

```

### Move LogAnalytics to new RG

Move LogAnalytics instance “ngsa-pre-west-log” from “ngsa-pre-west-log-rg” to “ngsa-pre-shared-rg” using Azure Portal or commands below.

```bash

# Query ID
az monitor log-analytics workspace show -n ngsa-pre-west-log -g ngsa-pre-west-log-rg --query id

# Move to a new RG
az resource move --destination-group ngsa-pre-shared-rg –ids {ID_OF_LOG_ANALYTICS}

```

**[Action] Investigate potential dashboard changes**

[IaC-Readme]: ../IaC/AKS/README.md
[IaC-Readme-DeployNGSA]: ../IaC/AKS/README.md#Deploy_NGSA_with_Helm
[Ngsa-cd-Readme]: https://github.com/retaildevcrews/ngsa-cd
[Ngsa-cd-Readme-Install]: https://github.com/retaildevcrews/ngsa-cd/#installation-instructions