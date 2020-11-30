#!/bin/bash
## TODO:
##   Put descriptive echo's in Color
##   Cleanup script [sem-done]
## Qurey
##   Zone and region needed to be selected for both smoker and ngsa

# Sane prog env:
set -o errexit -o pipefail -o noclobber # -o nounset
aze() { echo "executing: az $@";az "$@"; }
dockere() { echo "executing: docker $@";docker "$@"; }
kubectle() { echo "executing: kubectl $@";kubectl "$@"; }
# Functions
usage(){
    echo """
    $0 --ngsa-prefix basename123 [Optional Args/Flags]
    $0 -s azure-subs -n basename123 [Optional Args/Flags]

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
    -x | --set-k8s-context          Sets the kubernetes context for current user in $HOME/.kube/config
    -o | --smoke-it                 Set it to true or false to enable smokers deployment. Default: false
    -h | --help                     Show the usage
"""
}
echoc(){
    c=$1;shift;b=$(tput bold);r=$(tput setaf 1);g=$(tput setaf 2);b=$(tput setaf 4);y=$(tput setaf 3);d=$(tput sgr0)
    echo -e $(eval "echo \$${c}")"$@""${d}"
}
echo_exit(){
    ec=$1;shift;echo -e "\n$@" >&2
    exit $ec
}

[[ "$#" == 0 ]] && usage && exit 0
while (( "$#" )); do
    key="$1" val="$2"
    case "$key" in
        -s|--subscription)
        AZ_Sub=$val
        shift 2
        ;;

        -n|--ngsa-prefix)
        Ngsa_Base_Name=$val
        shift 2
        ;;

        -e|--env)
        Ngsa_Env=$val
        shift 2
        ;;

        -d|--domain)
        Ngsa_Domain_Name=$val
        shift 2
        ;;

        -m|--email)
        Ngsa_Email=$val
        shift 2
        ;;

        -k|--k8s-ver)
        Ngsa_K8S_VER=$val
        shift 2
        ;;

        -c|--node-count)
        Ngsa_Node_Count=$val
        shift 2
        ;;

        -r|--dns-rg)
        Ngsa_DNS_RG=$val
        shift 2
        ;;

        -o|--smoke-it)
        Ngsa_Smoke=yes
        shift
        ;;

        -i|--cosmos-key)
        Ngsa_Imdb_CM_Key=$val
        shift 2
        ;;

        -u|--cosmos-url)
        Ngsa_Imdb_CM_Url=$val
        shift 2
        ;;

        -l|--location)
        Ngsa_Location=$val
        shift 2
        ;;

        -x|--set-k8s-context)
        set_k8s_ctx=yes
        shift
        ;;

        -h|--help)
        usage && exit 0
        ;;

        -*|--*=) # unsupported flags
        echo_exit 1 "Error: Unsupported flag $1"
        ;;
    esac
done

# Check for naming pattern
pat_special_chars="[A-Z!@#$%^&*()+,.?\":{}|<> -]"
if [[ -z ${Ngsa_Base_Name} || ${#Ngsa_Base_Name} -lt 5 || \
    "${Ngsa_Base_Name}" =~ ${pat_special_chars} || "${Ngsa_Base_Name}" =~ "^[0-9]" ]]; then
    usage
    exit 1
fi

# Make sure we have docker, kubectl, helm and istioctl in $PATH
for i in az helm kubectl http docker istioctl; do
    [[ $(command -v $i) == "" ]] && echo_exit 1 "Make sure '$i' is installed and added to your \$PATH"
done

# Check if subscription is valid and available
if [[ ! -z ${AZ_Sub} ]]; then
    #subs_arg="--subscription ${AZ_Sub}"
    ! az account show -o table --subscription ${AZ_Sub} && echo_exit 1 "Subscription '${AZ_Sub}' not found"
    # If we are using separate subscription, then copy the config to tmp
    azure_cfg="$(mktemp -d)/"
    cp -r $HOME/.azure/* ${azure_cfg}
    export AZURE_CONFIG_DIR="${azure_cfg}"
    echo -e "\nAzure temp config dir: $AZURE_CONFIG_DIR"
    aze account set -s ${AZ_Sub} --output table
else
    echo -e "\nUsing selected subscription"
    aze account show -o table
fi
# Check if an email was provided if domain was specified 
if [[ ! -z ${Ngsa_Domain_Name} ]]; then
    [[ -z ${Ngsa_Email} ]] && echo_exit 1 "Provide an email '--email' for domain: $Ngsa_Domain_Name"
fi

# Se default values if optional env is unset
script_dir=$(dirname $0)
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

# Check if the RGs exists
echo -e "\nChecking if $Ngsa_App_RG or $Ngsa_Log_Analytics_RG resource group exists"
if [[ "$(az group exists -n $Ngsa_App_RG ${subs_arg})" == "true" || \
        "$(az group exists -n $Ngsa_Log_Analytics_RG ${subs_arg})" == "true" ]]; then
    echo_exit 1 "RG $Ngsa_Log_Analytics_RG and/or $Ngsa_App_RG exists. Choose a different prefix (-n)."
fi

echo -e "\nChecking if ${Ngsa_Name}-aks service principal exists"
if [[ ! $(az ad sp list --display-name "${Ngsa_Name}-aks" --query '[].appId' -o tsv) == '' ]]; then
    echo_exit 1 "Service principal ${Ngsa_Name}-aks already exists. Choose a different prefix (-n)."
fi

if [[ ! -z ${Ngsa_Imdb_CM_Key} || ! -z ${Ngsa_Imdb_CM_Url} ]]; then
    # Checking whether User provided both key and url
    if [[ -z ${Ngsa_Imdb_CM_Key} || -z ${Ngsa_Imdb_CM_Url} ]]; then
        echo_exit 1 "Provide both Cosmos Url (-u) and Cosmos Key (-i)"
    fi
    # Since we have Cosmos Key and Url we don't need Cosmos RG and Name
    # Dumb rudimentary check to make sure CosmosDB url exists
    # If it doesn't http will return non-zero
    http ${Ngsa_Imdb_CM_Url} > /dev/null
    [[ $? != 0 ]] && echo_exit "${Ngsa_Imdb_CM_Url} not valid"
    
    # After Cosmos Verification, create the resource groups
    aze group create -n $Ngsa_App_RG -l $Ngsa_Location -o table ${subs_arg}
    aze group create -n $Ngsa_Log_Analytics_RG -l $Ngsa_Location -o table ${subs_arg}
else
    # Check Cosmos name availability
    if [[ $(az cosmosdb check-name-exists -n "${Imdb_Name}" ${subs_arg}) == "true" ]]; then
        echo_exit "Cosmosdb ${Imdb_Name} exists in current subscription"
    fi
    
    #### Create and load sample data into Cosmos DB
    # create the resource groups
    aze group create -n $Ngsa_App_RG -l $Ngsa_Location -o table ${subs_arg}
    aze group create -n $Ngsa_Log_Analytics_RG -l $Ngsa_Location -o table ${subs_arg}
    aze group create -n $Imdb_RG -l $Imdb_Location -o table ${subs_arg}
    
    echo -e "\nCreating Cosmos DB server(this can take a while)..."
    # this command takes several minutes to run
    aze cosmosdb create -g $Imdb_RG -n $Imdb_Name -o table ${subs_arg}

    # create the database
    # 400 is the minimum --throughput (RUs)
    echo -e "\nCreaing SQL DB"
    aze cosmosdb sql database create -a $Imdb_Name -n $Imdb_DB -g $Imdb_RG --throughput 1000 -o table ${subs_arg}

    # create the container
    # /partitionKey is the partition key (case sensitive)
    echo -e "\nCreating SQL Container"
    aze cosmosdb sql container create -p /partitionKey -g $Imdb_RG -a $Imdb_Name -d $Imdb_DB -n $Imdb_Col -o table ${subs_arg}

    # run the IMDb Import app from Docker
    echo -e "\nPolulating SQL DB"
    Imdb_RW_Key=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv ${subs_arg})
    docker run -it --rm retaildevcrew/imdb-import $Imdb_Name ${Imdb_RW_Key} $Imdb_DB $Imdb_Col
    ## Finished Loading IMDB
    Ngsa_Imdb_CM_Key=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv ${subs_arg})
    Ngsa_Imdb_CM_Url=https://${Imdb_Name}.documents.azure.com:443/
fi

#### Create Log Analytics
Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"
echo -e "\nCreating Log Analytics Workspace"
aze extension add -n log-analytics
aze monitor log-analytics workspace create \
        -g $Ngsa_Log_Analytics_RG -l $Ngsa_Location \
        -n $Ngsa_Log_Analytics_Name -o table ${subs_arg}

#### Create the AKS Cluster
Ngsa_AKS_Name="${Ngsa_Name}-aks"
[[ -z ${Ngsa_K8S_VER} ]] && Ngsa_K8S_VER=1.18.8
[[ -z ${Ngsa_Node_Count} ]] && Ngsa_Node_Count=3
echo -e "\nCreating the NGSA Cluster."
echo    "K8s version: ${Ngsa_K8S_VER}"
echo    "Node Count: ${Ngsa_Node_Count}"
echo    "This can take a while..."
# this step usually takes 2-4 minutes
aze aks create -n $Ngsa_AKS_Name -g $Ngsa_App_RG -l $Ngsa_Location --enable-cluster-autoscaler --min-count ${Ngsa_Node_Count} --max-count $((Ngsa_Node_Count+3)) --node-count ${Ngsa_Node_Count} --kubernetes-version $Ngsa_K8S_VER --no-ssh-key -o table ${subs_arg}

# Setup a separate kubectl context if --set-k8s-context wasn't set
if [[ -z ${set_k8s_ctx} ]];then
    k8scfg_path=$(mktemp)
    echo -e "\n-- Not setting current K8s context --"
    echo -e "Using temporary kubeconfig: ${k8scfg_path}"
    echo -e "Use: 'AZURE_CONFIG_DIR=${AZURE_CONFIG_DIR} az aks get-credentials -n $Ngsa_AKS_Name -g $Ngsa_App_RG ${subs_arg}' to set this cluster as current k8s context"
else
    k8scfg_path=${HOME}/.kube/config
fi
aze aks get-credentials -n $Ngsa_AKS_Name -g $Ngsa_App_RG ${subs_arg} -f ${k8scfg_path}

# For helm, kubectl and istioctl
export KUBECONFIG=${k8scfg_path}
# Check kubectl access
echo -e "\nAvailable K8s Nodes:"
kubectl get nodes

# Add the required helm repositories
echo -e "\nAdd the required helm repositories"
helm repo add stable https://kubernetes-charts.storage.googleapis.com
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install the Istio Operator and Components on AKS
echo -e "\nInstall the Istio Operator and Components on AKS"
istioctl operator init
kubectl create namespace istio-system
kubectl apply -f $REPO_ROOT/IaC/AKS/cluster/manifests/istio/istio.aks.yaml
# One-line sleep-waiting on deployment deployment/istio-ingressgateway
echo -e "\nWaiting for Istio deployment..."
while ! kubectl wait --for=condition=available deployment/istio-ingressgateway -n istio-system ;do sleep 1;done
# Validate the Istio installation
kubectl get all --namespace istio-system

# Enable automatic sidecar injection in the ngsa namespace:
kubectl create namespace ngsa
kubectl label namespace ngsa istio-injection=enabled

INGRESS_PIP=$(kubectl --namespace istio-system get svc -l istio=ingressgateway -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')
echo -e "\nIngree PIP: ${INGRESS_PIP}"
# Install KEDA
KEDA_VERSION=2.0.0
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda --version $KEDA_VERSION --atomic

# Create Secrets for NGSA
Ngsa_Log_Analytics_Wid=$(az monitor log-analytics workspace show -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query customerId -o tsv ${subs_arg})
Ngsa_Log_Analytics_SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_Analytics_RG -n $Ngsa_Log_Analytics_Name --query primarySharedKey -o tsv ${subs_arg})
kubectl create secret generic ngsa-secrets --namespace ngsa --from-literal=CosmosDatabase=$Imdb_DB --from-literal=CosmosCollection=$Imdb_Col --from-literal=CosmosKey=${Ngsa_Imdb_CM_Key} --from-literal=CosmosUrl=${Ngsa_Imdb_CM_Url}

# Setup SSL/DNS with a registered domain if available
# If Domain name is not given, use the default one
if [[ -z ${Ngsa_Domain_Name} || ${Ngsa_Domain_Name} == 'nip.io' ]]; then
    Ngsa_Domain_Name=nip.io
    # set application endpoint
    export Ngsa_App_Endpoint="${INGRESS_PIP}.${Ngsa_Domain_Name}"
    cert_enabled=false
    export Ngsa_Https_App_Endpoint="http://${Ngsa_App_Endpoint}"
else
    export Ngsa_Email=$Ngsa_Email
    [[ -z ${Ngsa_DNS_RG} ]] && Ngsa_DNS_RG=dns-rg
    # Check if DNS resource group exists
    if [[ "$(az group exists -n $Ngsa_DNS_RG ${subs_arg})" == "false" ]]; then
        # If false, create DNS resource group
        aze group create -n $Ngsa_DNS_RG -l $Ngsa_Location -o table ${subs_arg}
    fi

    # Check if DNS Zone exists
    if ! az network dns zone show --name $Ngsa_Domain_Name -g $Ngsa_DNS_RG -o table ${subs_arg}; then
        # If not found, create the DNS Zone.
        aze network dns zone create -g $Ngsa_DNS_RG -n $Ngsa_Domain_Name -o table ${subs_arg}
    fi
    # Add DNS A record for the Istio ingress gateway.
    aze network dns record-set a add-record -g $Ngsa_DNS_RG -z $Ngsa_Domain_Name -n $Ngsa_Name -a $INGRESS_PIP -o table ${subs_arg}

    # Show the Azure nameservers for your DNS Zone.
    aze network dns zone show -n $Ngsa_Domain_Name -g $Ngsa_DNS_RG --query nameServers -o tsv ${subs_arg}

    ## Install Cert Manager
    cd $REPO_ROOT/IaC/AKS/cluster/manifests/cert-manager
    CERT_MANAGER_VERSION=1.0.3
    kubectl create namespace cert-manager
    echo -e "\nInstalling Cert-Manager"
    helm install cert-manager jetstack/cert-manager --namespace cert-manager --version "v${CERT_MANAGER_VERSION}" --set installCRDs=true --atomic
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
echo -e "\nDeploying NGSA with Helm"
cd $REPO_ROOT/IaC/AKS/cluster/charts/ngsa
# Use the helm-config.yaml file to configure the deployment
envsubst < helm-config.example.yaml >| helm-config.yaml
cd $REPO_ROOT/IaC/AKS/cluster/charts
helm install ngsa-aks ngsa -f "./ngsa/helm-config.yaml" --namespace ngsa --set cert.issuer=letsencrypt-prod --set cert.enabled=${cert_enabled} --atomic

# check the version endpoint
# you may get a timeout error, if so, just retry
http ${Ngsa_App_Endpoint}/version
## Running validation
# run the tests in a container
docker run -it --rm retaildevcrew/webvalidate --server $Ngsa_Https_App_Endpoint --base-url https://raw.githubusercontent.com/retaildevcrews/ngsa/main/TestFiles/ --files baseline.json
## Smoke Tests
if [[ ! -z ${Ngsa_Smoke} ]];then
    cd $REPO_ROOT/IaC/AKS/cluster/charts/

    echo -e "\nRunning smoke tests"
    kubectl create namespace ngsa-l8r
    cp -f ./loderunner/helm-config.example.yaml ./loderunner/helm-config.yaml
    helm install l8r loderunner -f ./loderunner/helm-config.yaml --namespace ngsa-l8r --atomic
    
    kubectl get pods --namespace ngsa-l8r
    # Fluent bit log forwarding
    echo -e "\nForwarding FluentBit log to Azure Log Analytics"
    cd $REPO_ROOT/IaC/AKS/cluster/charts

    kubectl create namespace fluentbit
    kubectl create secret generic fluentbit-secrets --namespace fluentbit --from-literal=WorkspaceId=${Ngsa_Log_Analytics_Wid} --from-literal=SharedKey=${Ngsa_Log_Analytics_SharedKey}

    helm install fluentbit fluentbit --namespace fluentbit --atomic

    # Verify the fluentbit pod is running
    kubectl get pod --namespace fluentbit
fi
#echo -e "Removing temp dirs ${rmdirs[@]}"
#for rd in ${rmdirs};do rm -r ${rd}; done