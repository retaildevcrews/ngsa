# Setup k8s IaaS

> This will setup a single node k8s cluster

## Create VM

Create your VM per instructions in [Bare Metal Setup](setup-bare-metal-vm.md)

## setup k8s

### Export your public IP address

```bash

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

# make sure PIP is set correctly
echo $PIP

# install k8s controller
sudo kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address $PIP

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

> this is not required if you only run `in-memory.yml`

```bash

# add az cli extension
az extension add --name log-analytics

# delete if necessary - you can safely ignore the not exists error
kubectl delete secret ngsa-secrets

# create from key vault
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosDatabase) \
  --from-literal=CosmosCollection=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosCollection) \
  --from-literal=CosmosKey=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosKey) \
  --from-literal=CosmosUrl=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosUrl) \
  --from-literal=AppInsightsKey=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name AppInsightsKey) \
  --from-literal=WorkspaceId=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name WorkspaceId) \
  --from-literal=SharedKey=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name SharedKey)

# display the secrets (base 64 encoded)
kubectl get secret ngsa-secrets -o jsonpath='{.data}'

# if you need to update a secret
kubectl create secret generic ngsa-secrets \
  --from-literal=foo=bar \
  --dry-run=client -o yaml | kubectl apply -f -

```

## Deploy ngsa

```bash

# create the pod and Cluster IP service
# Choose one

# use Cosmos DB
k apply -f ngsa.yml

# use in-memory DB
k apply -f in-memory.yml

# retry until you get the startup message
k logs ngsa

# to test without SSL
k expose service ngsa --type=LoadBalancer --port=80 --target-port=4120 --name ngsa-lb

# make sure the public IP is exposed
k get all

# test the public IP
curl $PIP/version

# delete test service
k delete svc ngsa-lb

# create load balancer
# this requires SSL to be setup correctly
k apply -f lb.yml

# check status
# wait for external IP to be visible
k get svc

# test the LB
# will return an https redirect
http ${PIP}

```
