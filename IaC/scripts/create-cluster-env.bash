#!/bin/bash
# TODO:
#   Put descriptive echo's in Color
#   Separate CosmosDB resource
#   Check Email if custom domain 
# Qurey
#   Zone and region needed to be selected for both smoker and ngsa
#   Smoker README up, should pass as Array
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
      Ngsa_Email        Required Email if domain name custom
    Ngsa_Location       Location where the resources will be created. Default: westus2
    Ngsa_K8S_VER        Kubernetes version used. Default: 1.18.8
                        Use 'az aks get-versions -l westus2 -o table' to get supported versions
    Ngsa_Node_Count     Cluster Node Count. Default: 3
    Ngsa_DNS_RG         DNS Resource group name. Default: dns-rg
    Ngsa_Smoke          Set it to true or false to enable smokers deployment. Default: false
    Ngsa_Imdb_CM_Key    Empty by default. In case users want to use their own CosmosDB
      Ngsa_Imdb_CM_Url  Empty by default. In case users want to use their own CosmosDB
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
for i in az helm kubectl http docker istioctl; do
    [[ $(command -v $i) == "" ]] && echo "$i not found in \$PATH" && exit 1
done

# Check if subscription is valid and available
subs_arg="--subscription ${AZ_Sub}"

az account show -o table ${subs_arg}
[[ $? != 0 ]] && echo "Subscription '${AZ_Sub}' not found" && exit 1

# Se default values if optional env is unset
REPO_ROOT=$(realpath ${script_dir}/../../)
[[ -z ${Ngsa_Env} ]] && Ngsa_Env=dev
[[ -z ${Ngsa_Location} ]] && Ngsa_Location=westus2

Ngsa_Name="${Ngsa_Base_Name}-${Ngsa_Env}"

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
if [[ ! -z ${Ngsa_Imdb_CM_Key} || ! -z ${Ngsa_Imdb_CM_Url} ]]; then
    [[ -z ${Ngsa_Imdb_CM_Key} || -z ${Ngsa_Imdb_CM_Url} ]] && \
        echo "Provide both Ngsa_Imdb_CM_Key and Ngsa_Imdb_CM_Url variable"
        exit 1
    # Since we have Cosmos Key and Url we don't need Cosmos RG and Name
    # Dumb rudimentary check to make sure CosmosDB url exists
    # If it doesn't http will return non-zero
    http ${Ngsa_Imdb_CM_Url} > /dev/null
    [[ $? != 0 ]] && echo "${Ngsa_Imdb_CM_Url} not valid" && exit 1
else
    # Check Cosmos name availability
    if [[ $(az cosmosdb check-name-exists -n "${Imdb_Name}" ${subs_arg}) == "true" ]]; then
        echo "Cosmosdb ${Imdb_Name} exists in current subscription"
        exit 1
    fi
    Imdb_RW_Key='az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv ${subs_arg}'

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
    Ngsa_Imdb_CM_Key=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv ${subs_arg})
    Ngsa_Imdb_CM_Url=https://${Imdb_Name}.documents.azure.com:443/
fi


#### Create Log Analytics
Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"
az extension add -n log-analytics
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

INGRESS_PIP=$(kubectl --namespace istio-system get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')

# Install KEDA
KEDA_VERSION=2.0.0
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda --version $KEDA_VERSION --atomic

# Create Secrets for NGSA
Ngsa_Log_Analytics_Wid=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv ${subs_arg})
Ngsa_Log_Analytics_SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv ${subs_arg})
kubectl create secret generic ngsa-aks-secrets \
    --namespace ngsa \
    --from-literal=CosmosDatabase=$Imdb_DB \
    --from-literal=CosmosCollection=$Imdb_Col \
    --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv ${subs_arg}) \
    --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
    --from-literal=WorkspaceId=${Ngsa_Log_Analytics_Wid} \
    --from-literal=SharedKey=${Ngsa_Log_Analytics_SharedKey}

# Setup SSL/DNS with a registered domain if available
if [[ -z ${Ngsa_Domain_Name} || ${Ngsa_Domain_Name} == 'nip.io' ]]; then
    Ngsa_Domain_Name=nip.io
    # set application endpoint
    export Ngsa_App_Endpoint="${INGRESS_PIP}.${Ngsa_Domain_Name}"
    cert_enabled=false
    export Ngsa_Https_App_Endpoint="http://${Ngsa_App_Endpoint}"
else
    # Check if  email is set
    export Ngsa_Email=$Ngsa_Email
    [[ -z ${Ngsa_Email }]] && echo "Provide Ngsa_Email" && exit 1
    [[ -z ${Ngsa_DNS_RG} ]] && Ngsa_DNS_RG=dns-rg

    # Check if DNS resource group exists
    az group exists -n $Ngsa_DNS_RG ${subs_arg}

    # If false, create DNS resource group
    az group create -n $Ngsa_DNS_RG -l $Ngsa_Location ${subs_arg}

    # Check if DNS Zone exists
    az network dns zone show --name $Ngsa_Domain_Name -g $Ngsa_DNS_RG -o table ${subs_arg}

    # If not found, create the DNS Zone.
    az network dns zone create -g $Ngsa_DNS_RG -n $Ngsa_Domain_Name ${subs_arg}

    # Add DNS A record for the Istio ingress gateway.
    az network dns record-set a add-record -g $Ngsa_DNS_RG -z $Ngsa_Domain_Name -n $Ngsa_Name -a $INGRESS_PIP ${subs_arg}

    # Show the Azure nameservers for your DNS Zone.
    az network dns zone show -n $Ngsa_Domain_Name -g $Ngsa_DNS_RG --query nameServers -o tsv ${subs_arg}

    ## Install Cert Manager
    cd $REPO_ROOT/IaC/AKS/cluster/manifests/cert-manager
    CERT_MANAGER_VERSION=1.0.3
    kubectl create namespace cert-manager
    helm install cert-manager jetstack/cert-manager \
        --namespace cert-manager \
        --version "v${CERT_MANAGER_VERSION}" \
        --set installCRDs=true --atomic
    # wait for the cert manager pods to be ready
    kubectl get pods --namespace cert-manager
    
    # Create a staging and production ClusterIssuer for cert-manager
    # Use the staging ClusterIssuer for testing. Once ready, use the production resource.
    envsubst < "clusterissuer.yaml" | kubectl apply -f -

    # set application endpoint
    export Ngsa_App_Endpoint="${Ngsa_Name}.${Ngsa_Domain_Name}"
    cert_enabled=true
    Ngsa_Https_App_Endpoint="https://${Ngsa_App_Endpoint}"
fi

## Deploy NGSA with Helm
# Directly installing PROD cert issuer
cd $REPO_ROOT/IaC/AKS/cluster/charts/ngsa
# Use the helm-config.yaml file to configure the deployment
envsubst < helm-config.example.yaml > helm-config.yaml
cd $REPO_ROOT/IaC/AKS/cluster/charts
helm install ngsa-aks ngsa \
    -f "./ngsa/helm-config.yaml" \
    --namespace ngsa --set cert.issuer=letsencrypt-prod \
    --set cert.enabled=${cert_enabled} --atomic

# check the version endpoint
# you may get a timeout error, if so, just retry
http ${Ngsa_App_Endpoint}/version
## Running validation
# run the tests in a container
# TODO: $Ngsa_Https_App_Endpoint
docker run -it --rm retaildevcrew/webvalidate \
    --server $Ngsa_Https_App_Endpoint \
    --base-url https://raw.githubusercontent.com/retaildevcrews/ngsa/main/TestFiles/ \
    --files baseline.json
## Smoke Tests
if [[ ! -z ${Ngsa_Smoke} ]];then
    cd $REPO_ROOT/IaC/AKS/cluster/charts

    kubectl create namespace ngsa-smoker
    kubectl create secret generic ngsa-smoker-secrets \
        --namespace ngsa-smoker \
        --from-literal=WorkspaceId=${Ngsa_Log_Analytics_Wid} \
        --from-literal=SharedKey=${Ngsa_Log_Analytics_SharedKey}

    helm install ngsa-smoker smoker --namespace ngsa-smoker \
        --set ingressURLs={"${Ngsa_Https_App_Endpoint}"} --atomic

    # Verify the pods are running
    kubectl get pods --namespace ngsa-smoker
fi