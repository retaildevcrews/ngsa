# Setup k8s IaaS

> This will setup a single node k8s cluster for development

## Create IMDb Cosmos DB

> TODO - I think we should move this to a separate section / file
> focus the initial quick start on in-memory only

Create IMDb Cosmos DB and load sample data per instructions [here](https://github.com/retaildevcrews/imdb)

```bash

# these variables are set during IMDb setup and used below
# you can set the variables to an existing Cosmos DB instance
env | grep Imdb_

#export Imdb_Name=YourCosmosName
#export Imdb_DB=imdb
#export Imdb_Col=movies
#export Imdb_RG=$Imdb_Name-rg-cosmos
#export Imdb_Location="centralus"

```

## setup Log Analytics

> TODO - I think we should move this to a separate section / file
> focus the initial quick start on in-memory + stdout only

```bash

# add az cli extension
az extension add --name log-analytics

# set environment variables
# You can use an existing Log Analytics instance
export Ngsa_Log_RG=$Imdb_Name-rg-logs
export Ngsa_Log_Name=$Imdb_Name

az group create -n $Ngsa_Log_RG -l $Imdb_Location

az monitor log-analytics workspace create -g $Ngsa_Log_RG -n $Ngsa_Log_Name -l $Imdb_Location

```

## Create Azure VM

Create your Azure VM per instructions at [Azure Kubernetes Development Cluster](https://github.com/retaildevcrews/akdc)

## SSH into the Azure VM

```bash

# AKDC_IP is set during the previous step
# the -L allows you to forward your port from the dev cluster via SSH tunneling
ssh -L 4120:127.0.0.1:4120 akdc@${AKDC_IP}

# clone this repository
git clone https://github.com/retaildevcrews/ngsa

# change to the correct directory
cd ngsa/IaC/BareMetal/app

# TODO - remove this
git checkout containerd
git pull

# verify kubernetes is running
kubectl get all --all-namespaces

```

## Deploy the NGSA app

Follow the deployment instructions in [app](app/README.md) to deploy ngsa

## Deploy fluentbit (log forwarding)

Follow the deployment instructions in [fluentbit](fluentbit/README.md) to debug ngsa with fluent bit and Azure Log Analytics
