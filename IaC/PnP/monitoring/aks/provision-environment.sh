#!/bin/bash
export regionName=eastus2
export TFRG_NAME=mon-dev-rg-infra
export TFSA_NAME=mondevngsainfra
export TFCI_NAME=devtfstate
  
function parse_args()
{
  red=`tput setaf 1`
  green=`tput setaf 2`
  yellow=`tput setaf 3`
  reset=`tput sgr0`
  die() { echo "$*" >&2; exit 2; }  # complain to STDERR and exit with error
  needs_arg() { if [ -z "$OPTARG" ]; then die "No arg for --$OPT option"; fi; }

  # PARSE ARGUMENTS
  # -? - whatif
  # -f - first run

  WHAT_IF=0
  FIRST_RUN=0
  INIT=0
  while getopts "hiw" opt; do
    case ${opt} in
      h ) # process option h
        echo "Usage: provision-environment.sh [-h] [-i] [-w]"
        echo "       -h  this help message"
        echo "       -i  initialize Terraform"
        echo "       -w  what-if"
        exit 1
        ;;
      i ) # initialize option
        INIT=1
        ;;
      w ) # process option w
        WHAT_IF=1
        ;;
      ? ) 
        echo "Usage: provision-environment.sh [-h] [-i] [-w]"
        exit 1
        ;;
    esac
  done

}

function ensure_state_environment()
{
    echo "Ensuring resource group $TFRG_NAME exists"
    if ! (az group list --output tsv | grep -q -s $TFRG_NAME || az group create -l $regionName -n $TFRG_NAME -o table) 
    then
        echo "Failed to create resource group $TFRG_NAME"
        exit 1
    fi

    echo "Ensuring storage account $TFSA_NAME exists"
    if ! (az storage account list --output tsv | grep -q -s $TFSA_NAME)
    then
        if ! (az storage account create --resource-group $TFRG_NAME --name $TFSA_NAME --sku Standard_LRS --encryption-services blob -o table)
        then
            echo "ERROR: Failed to create Storage Account"
            exit 1
        fi
        sleep 20s 
    fi

    # retrieve storage account access key
    echo "Retrieving access key for $TFSA_NAME"
    if ! SA_ACCESS_KEY=$(az storage account keys list --resource-group $TFRG_NAME --account-name $TFSA_NAME --query [0].value -o tsv)
    then
        echo "ERROR: Failed to Retrieve Storage Account Access Key"
        exit 1
    fi

    echo "Ensuring container $TFSA_NAME/$TFCI_NAME exists"
    if ! (az storage container list --account-name $TFSA_NAME --auth-mode login | grep -q -s $TFCI_NAME || az storage container create --name $TFCI_NAME --account-name $TFSA_NAME --account-key $SA_ACCESS_KEY -o table)
    then
        echo "ERROR: Failed to Retrieve Storage Container"
        exit 1
    fi
}

function initialize_terraform()
{
    if [ $INIT -eq 1 ]
    then
      # The TF variables are initialized in _prepare-terraform
        terraform init -upgrade -reconfigure -backend-config="resource_group_name=${TFRG_NAME}" -backend-config="storage_account_name=${TFSA_NAME}" -backend-config="container_name=${TFCI_NAME}" -backend-config="key=${SA_ACCESS_KEY}"
    fi
}

############################### MAIN ###################################
parse_args "$@"

ensure_state_environment

initialize_terraform

configuration_folder=online/aks_secure_baseline/configuration
parameter_files=$(find $configuration_folder | grep .tfvars | sed 's/.*/-var-file &/' | xargs)

if [[ $WHAT_IF -eq 1 ]]
then
    eval terraform plan ${parameter_files}
else
    eval terraform apply ${parameter_files}
fi
echo "Done."