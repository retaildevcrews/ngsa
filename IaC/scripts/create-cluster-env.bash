#!/bin/bash
## TODO: Put descriptive echo's

# Sane prog env:
set -o errexit -o pipefail -o noclobber # -o nounset
script_dir=$(dirname $0)

# Functions
usage(){
    echo """
Export env vars(case sensitive) or inline them with the script
Ex. export AZ_Sub=bartr-abcd && export Ngsa_Base_Name=uniquename
    $0
Ex. AZ_Sub=bartr-abcd Ngsa_Base_Name=uniquename $0

Required Env Vairables:
    AZ_Sub              Azure Subscription Name or ID
    Ngsa_Base_Name      This will be the prefix for all resources
                        Do not include punctuation - only use a-z and 0-9
                        must be at least 5 characters long
                        must start with a-z (only lowercase)
Optional Env Variables:
    Ngsa_Env            Environemnt Type. Default: dev (See README.md for other values)
    Ngsa_Domain_Name    Registered Domain Name. Default: nip.io
    Ngsa_Location       Location where the resources will be created. Default: westus2
    Ngsa_K8S_VER        Kubernetes version used. Default: 1.18.8
                        Use 'az aks get-versions -l westus2 -o table' to get supported versions
    Ngsa_Node_Count     Cluster Node Count. Default: 3
"""
}
[[ "$1" == '-h' || "$1" == '--help' ]] && usage && exit 0

# Check for naming pattern
pat_special_chars="[A-Z!@#$%^&*()+,.?\":{}|<> -]"
if [[ -z ${AZ_Sub} || -z ${Ngsa_Base_Name} || 
    ${#Ngsa_Base_Name} -lt 5 || 
    "${Ngsa_Base_Name}" =~ ${pat_special_chars} || "${Ngsa_Base_Name}" =~ "^[0-9]" ]]; then
usage
exit -1
fi

# Make sure we have docker, kubectl, helm and istioctl in $PATH
for i in helm kubectl docker istioctl; do
    [[ $(command -v $i) == "" ]] && echo "$i not found in \$PATH" && exit 1
done

# Check if subscription is valid and available
subs_arg="--subscription ${AZ_Sub}"
az account show -o table ${subs_arg}
[[ $? != 0 ]] && echo "Subscription '${AZ_Sub}' not found" && exit 1

# Se default values if optional env is unset
REPO_ROOT=$(realpath ${script_dir}/../../)
[[ -z ${Ngsa_Env} ]] && Ngsa_Env=dev
[[ -z ${Ngsa_Domain_Name} ]] && Ngsa_Domain_Name=nip.io
[[ -z ${Ngsa_Location} ]] && Ngsa_Location=westus2

Ngsa_Name="${Ngsa_Base_Name}-${Ngsa_Env}"
# Check Cosmos name availability
if [[ $(az cosmosdb check-name-exists -n "${Ngsa_Name}-cosmos" ${subs_arg}) == "true" ]]; then
    echo "Cosmosdb ${Ngsa_Name}-cosmos exists in current subscription"
    exit 1
fi

# set application endpoint
Ngsa_App_Endpoint="${Ngsa_Name}.${Ngsa_Domain_Name}"

# resource group names
Imdb_Name="${Ngsa_Name}-cosmos"
Ngsa_App_RG="${Ngsa_Name}-app-rg"
Ngsa_Log_Analytics_RG="${Ngsa_Name}-log-rg"
Imdb_RG="${Ngsa_Name}-cosmos-rg"

# Cosmos DB env vars
# these will be explained in the Cosmos DB setup step
Imdb_Location=$Ngsa_Location
Imdb_DB=imdb # These are default values for Imdb
Imdb_Col=movies
Imdb_RW_Key='az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv'

# create the resource groups
az group create -n $Ngsa_App_RG -l $Ngsa_Location ${subs_arg}
az group create -n $Ngsa_Log_Analytics_RG -l $Ngsa_Location ${subs_arg}

#### Create and load sample data into Cosmos DB
az group create -n $Imdb_RG -l $Imdb_Location ${subs_arg}
# create the Cosmos DB server
# this command takes several minutes to run
az cosmosdb create -g $Imdb_RG -n $Imdb_Name ${subs_arg}

# create the database
# 400 is the minimum --throughput (RUs)
az cosmosdb sql database create -a $Imdb_Name -n $Imdb_DB -g $Imdb_RG --throughput 1000 ${subs_arg}

# create the container
# /partitionKey is the partition key (case sensitive)
az cosmosdb sql container create -p /partitionKey -g $Imdb_RG -a $Imdb_Name -d $Imdb_DB -n $Imdb_Col ${subs_arg}

# run the IMDb Import app from Docker
docker run -it --rm retaildevcrew/imdb-import $Imdb_Name $(eval $Imdb_RW_Key) $Imdb_DB $Imdb_Col
## Finished Loading IMDB

#### Create Log Analytics
Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"
# Add Log Analytics extension
az extension add -n log-analytics ${subs_arg}
# create Log Analytics for the webv clients
az monitor log-analytics workspace create -g \
        $Ngsa_Log_Analytics_RG -l $Ngsa_Location \
        -n $Ngsa_Log_Analytics_Name -o table ${subs_arg}

#### Create the AKS Cluster
Ngsa_AKS_Name="${Ngsa_Name}-aks"
[[ -z ${Ngsa_K8S_VER} ]] && Ngsa_K8S_VER=1.18.8
[[ -z ${Ngsa_Node_Count} ]] && Ngsa_Node_Count=3
echo "Creating the NGSA Cluster. K8s version: ${Ngsa_K8S_VER}, Node Count: ${Ngsa_Node_Count}"

# this step usually takes 2-4 minutes
az aks create --name $Ngsa_AKS_Name --resource-group $Ngsa_App_RG \
        --location $Ngsa_Location --enable-cluster-autoscaler \
        --min-count ${Ngsa_Node_Count} --max-count $((Ngsa_Node_Count+3)) \
        --node-count ${Ngsa_Node_Count} \
        --kubernetes-version $Ngsa_K8S_VER --no-ssh-key ${subs_arg}

# Setup Kubectl context
az aks get-credentials -n $Ngsa_AKS_Name -g $Ngsa_App_RG ${subs_arg}

# Check kubectl access
kubectl get nodes

# Make sure user has Helm and istio installed

# Add the required helm repositories
helm repo add stable https://kubernetes-charts.storage.googleapis.com
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install the Istio Operator and Components on AKS
istioctl operator init
kubectl create namespace istio-system
kubectl apply -f $REPO_ROOT/IaC/AKS/cluster/manifests/istio/istio.aks.yaml

# Validate the Istio installation
kubectl get all --namespace istio-system

# Enable automatic sidecar injection in the ngsa namespace:
kubectl create namespace ngsa
kubectl label namespace ngsa istio-injection=enabled

export INGRESS_PIP=$(kubectl --namespace istio-system get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')

# Install KEDA
KEDA_VERSION=2.0.0
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda --version $KEDA_VERSION

# Create Secrets for NGSA
kubectl create secret generic ngsa-aks-secrets \
    --namespace ngsa \
    --from-literal=CosmosDatabase=$Imdb_DB \
    --from-literal=CosmosCollection=$Imdb_Col \
    --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
    --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
    --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv) \
    --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv)

