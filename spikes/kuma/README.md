# Kuma service mesh spike

TODO: intro

## Prerequisites

TODO: prereqs. az cli and kuma cli

tested in ubuntu(WSL)

## Download Kuma CLI

TODO: download instructions

## Architecture diagram

TODO: high level diagram of final setup

## Setup

### Create clusters

setup variables

```bash

# git clone and cd in spikes/kuma

LOCATION=centralus

# get the available versions
az aks get-versions -l $LOCATION -o table

# choose a version
AKS_VERSION=1.19.3

# create a resource group
RG_NAME=kuma

```

create clusters

```bash

az group create -n $RG_NAME -l $LOCATION

# create clusters

# create a cluster for the main kuma zone.
az aks create --name main --resource-group $RG_NAME --location $LOCATION --enable-cluster-autoscaler --min-count 1 --max-count 3 --node-count 1 --kubernetes-version 1.19.3 --no-ssh-key

# create a cluster for the first remote kuma zone
az aks create --name remote1 --resource-group $RG_NAME --location $LOCATION --enable-cluster-autoscaler --min-count 1 --max-count 3 --node-count 1 --kubernetes-version 1.19.3 --no-ssh-key

# create a cluster for the second remote kuma zone
az aks create --name remote2 --resource-group $RG_NAME --location $LOCATION --enable-cluster-autoscaler --min-count 1 --max-count 3 --node-count 1 --kubernetes-version 1.19.3 --no-ssh-key

```

get credentials to clusters

```bash

# get credentials
az aks get-credentials --resource-group $RG_NAME --name main

az aks get-credentials --resource-group $RG_NAME --name remote1

az aks get-credentials --resource-group $RG_NAME --name remote2

```

### Setup service mesh

setup main kuma zone

```bash

# switch context to main cluster
kubectl config use-context main

# install kuma global control plane in the main kuma zone
kumactl install control-plane --mode=global | kubectl apply --context main -f -

# check that the kuma loadbalancer is available and has an external ip address
kubectl get service kuma-global-remote-sync --namespace kuma-system --context main

# get external ip address of global service
LOAD_BALANCER_ADDRESS=$(kubectl get service kuma-global-remote-sync --namespace kuma-system --context main -o jsonpath="{.status.loadBalancer.ingress[0].ip}")

# kuma global control plane address in main kuma zone
GLOBAL_ADDRESS="grpcs://${LOAD_BALANCER_ADDRESS}:5685"

# enable mtls to allow cross cluster traffic
kubectl apply -f mesh.yaml --context main

```

setup remote1 kuma zone

```bash

# switch context to remote1 cluster
kubectl config use-context remote1

# install kuma remote control plane in remote kuma zones
kumactl install control-plane --mode=remote --zone=remote1 --ingress-enabled --kds-global-address $GLOBAL_ADDRESS | kubectl apply --context remote1 -f -

# install kuma DNS in remote kuma zones
kumactl install dns | kubectl apply --context remote1 -f -

```

setup remote2 kuma zone

```bash

# switch context to remote2 cluster
kubectl config use-context remote2

# install kuma remote control plane in remote kuma zones
kumactl install control-plane --mode=remote --zone=remote2 --ingress-enabled --kds-global-address $GLOBAL_ADDRESS | kubectl apply --context remote2 -f -

# install kuma DNS in remote kuma zones
kumactl install dns | kubectl apply --context remote2 -f -

```

### Deploy apps in mesh

deploy different versions of ngsa in the different kuma zones.

```bash

# deploy a specific version of ngsa to remote1 cluster
# the different versions will be used to test cross cluster routing
NGSA_IMAGE_TAG="beta-0.0.8-1215-2142-117" envsubst < ngsa.yaml | kubectl apply --context remote1 -f -

# make sure pods are running
kubectl get pods --namespace kuma-spike --context remote1

# deploy a different specific version of ngsa to remote2 cluster
NGSA_IMAGE_TAG="beta-0.0.8-1215-1946-115" envsubst < ngsa.yaml | kubectl apply --context remote2 -f -

# make sure pods are running
kubectl get pods --namespace kuma-spike --context remote2

```

test app in remote1 zone

```bash

# connect to the ngsa service in the remote1 cluster
kubectl port-forward svc/ngsa-memory 4120:4120 --namespace kuma-spike --context remote1

# in another terminal or browser, check the app version
# http://localhost:4120/version

curl http://localhost:4120/version

```

test app in remote2 zone

```bash

# connect to the ngsa service in the remote1 cluster
kubectl port-forward svc/ngsa-memory 4120:4120 --namespace kuma-spike --context remote2

# in another terminal or browser, check the app version
# http://localhost:4120/version

curl http://localhost:4120/version

```

### Traffic routing

add debugging pod to mesh

```bash

# add a debugging pod to the remote1 zone
kubectl apply -f debug.yaml --context remote1

# exec into the debug container
kubectl exec -it debug --namespace kuma-spike --context remote1 --container debug -- /bin/sh

```

inside the container

```bash

# install curl
apk --update add curl

# send traffic to the ngsa-memory service
watch -n 1 curl http://ngsa-memory:4120/version

# notice that the "appVersion" field is changing between the different versions that were installed in the remote kuma zones.

# keep this terminal open

```

update the balance of traffic between the kuma zones. open a new terminal.

```bash

# add a label to the ngsa pods. these labels will be used for routing

# label remote1 ngsa pod
kubectl label pod ngsa-memory --namespace kuma-spike kuma-zone=remote1 --context remote1

# label remote2 ngsa pod
kubectl label pod ngsa-memory --namespace kuma-spike kuma-zone=remote2 --context remote2

```

use [traffic_route.yaml](./traffic_route.yaml) to adjust traffic in the mesh. this traffic policy is set to send all traffic to the service that is running in the remote1 kuma zone.

```yaml
...
  conf:
    split:
      - weight: 100
        destination:
          kuma.io/service: ngsa-memory_kuma-spike_svc_4120
          kuma-zone: "remote1"
      - weight: 0
        destination:
          kuma.io/service: ngsa-memory_kuma-spike_svc_4120
          kuma-zone: "remote2"
```

```bash

# apply a traffic route to the global control plane in the main zone
kubectl apply -f traffic_route.yaml --context main

# in the previous terminal, the one connected to the debug container, notice that the version of the app is no longer changing. it is now fixed to the version running in remote1.

```

edit the weights in traffic_route.yaml, and observe the behavior in the debug container. for example, change the remote1 weight to 0 and remote2 to 100.

### Kuma UI

open Kuma UI

```bash

# connect to the global kuma control plane in the main zone
kubectl port-forward svc/kuma-control-plane --namespace kuma-system 5681:5681 --context main

# open http://127.0.0.1:5681/gui/ in browser to view UI

```
