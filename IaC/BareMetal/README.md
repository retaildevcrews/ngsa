# Setup k8s IaaS

> This will setup a single node k8s cluster

## Create IMDb Cosmos DB

Create IMDb Cosmos DB and load sample data per instructions [here](https://github.com/retaildevcrews/imdb)

```bash

# these variables are set during IMDb setup and used below
env | grep Imdb_

#export Imdb_Name=YourCosmosName
#export Imdb_DB=imdb
#export Imdb_Col=movies
#export Imdb_RG=$Imdb_Name-rg-cosmos
#export Imdb_Location="centralus"

```

## setup Log Analytics

```bash

# add az cli extension
az extension add --name log-analytics

# set environment variables
export Ngsa_Log_RG=$Imdb_Name-rg-logs
export Ngsa_Log_Name=$Imdb_Name

az group create -n $Ngsa_Log_RG -l $Imdb_Location

az monitor log-analytics workspace create -g $Ngsa_Log_RG -n $Ngsa_Log_Name -l $Imdb_Location

```

## Create VM

Create your VM per instructions in [Bare Metal Setup](setup-bare-metal-vm.md)

## setup k8s

### Export your public IP address

```bash

# check if it's set correctly
echo $PIP

# set if necessary
export PIP=YourPublicIPAddress

```

### Reset cluster if necessary

```bash

# reset your cluster
sudo kubeadm reset -f

```

### Initialize cluster

```bash

# make sure you're in the ngsa/IaC/BareMetal directory
cd IaC/BareMetal

# make sure PIP is set correctly
echo $PIP

# install k8s controller
sudo kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address $PIP

### WARNING ###
# This will delete your existing kubectl configuration
# Make sure to back up or merge manually
###############

# setup your config file
sudo rm -rf $HOME/.kube
mkdir -p $HOME/.kube
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown -R $(id -u):$(id -g) $HOME/.kube

# add flannel network overlay
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml --namespace=kube-system

# optional - add the taint to schedule normal pods on the control plane
# this let you run a "one node" cluster for `development`
k taint nodes --all node-role.kubernetes.io/master-

# patch kube-proxy for metal LB
kubectl get configmap kube-proxy -n kube-system -o yaml | \
sed -e "s/strictARP: false/strictARP: true/" | \
sed -e 's/mode: ""/mode: "ipvs"/' | \
kubectl apply -f - -n kube-system

## Install metal LB
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.4/manifests/namespace.yaml
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.4/manifests/metallb.yaml
kubectl create secret generic -n metallb-system memberlist --from-literal=secretkey="$(openssl rand -base64 128)"

# create config map
sed -e "s/{PIP}/${PIP}/g" metalLB.yml | k apply -f -

```

## Set ngsa secrets

```bash

# delete if necessary - you can safely ignore the not exists error
kubectl delete secret ngsa-secrets

# create from key vault
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_RG -n $Ngsa_Log_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_RG -n $Ngsa_Log_Name --query primarySharedKey -o tsv)
  
# display the secrets (base 64 encoded)
kubectl get secret ngsa-secrets -o jsonpath='{.data}'

# if you need to update a secret
kubectl create secret generic ngsa-secrets \
  --from-literal=foo=bar \
  --dry-run=client -o yaml | kubectl apply -f -

```

## Deploy ngsa

Follow the deployment instructions in [app](app/README.md) to deploy ngsa

## Debug fluentd

Follow the deployment instructions in [fluentd/dbg](fluentd/dbg/README.md) to debug ngsa with fluentd and Azure Log Analytics

## Debug fluentbit

Follow the deployment instructions in [fluentbit/dbg](fluentbit/dbg/README.md) to debug ngsa with fluent-bit and Azure Log Analytics
