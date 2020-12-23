# k8s Setup (bare metal)

> Setup a k8s bare metal cluster

- VM Requirements
  - minimum 2 GB / 2 core for k8s controller
  - make sure to use `Ubuntu 18.04`
  - copy the setup script below and paste into the `User data` section when creating VMs

## Azure setup

### Prerequisites

- Azure CLI ([download](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest))

### Login to Azure

```bash

# login to Azure
az login

az account list -o table

# select subscription (if necesary)
az account set -s YourSubscriptionName

```

### Create Resource Group

```bash

# set your resource group name and location

export NGSA_RG=YourRGName
export NGSA_LOC=westus2

# Create a resource group

az group create -l $NGSA_LOC -n $NGSA_RG

```

### Create VM

```bash

# Create an Ubuntu VM and install k8s

az vm create \
  -g $NGSA_RG \
  --admin-username ngsa \
  -n ngsa \
  --size standard_d2s_v3 \
  --nsg-rule SSH \
  --image Canonical:UbuntuServer:18.04-LTS:latest \
  --os-disk-size-gb 128 \
  --generate-ssh-keys \
  --custom-data startup.sh \
  --query publicIpAddress -o tsv

# This will output an IP address

ssh ngsa@IPAddressFromOutput

```

## Validation

From the VM

```bash

# this will tell you when the user data script is done
cat status

# make sure everything is up to date
sudo apt update
sudo apt upgrade -y

# reboot
sudo shutdown -r now

# ssh back into the VM

# return to the readme for k8s setup

```

## User Data

- For non-Azure VM
  - copy and paste `install.sh` into "user data" when you create a VM
  - set Azure=false if you need to create a non-root user
