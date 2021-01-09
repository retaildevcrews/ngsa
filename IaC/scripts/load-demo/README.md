# Load Test Demo

Instructions for running a load test demo

## Setup

### Prep

- az CLI is installed and logged in correctly
  - Correct Azure subscription is selected
- kubectl is installed
- Clone this repo and change to this directory

### Add AKS clusters to .kube/config

```bash

# get credentials for each cluster
az aks get-credentials -g ngsa-pre-central-app-rg -n ngsa-pre-central-aks
az aks get-credentials -g ngsa-pre-east-app-rg -n ngsa-pre-east-aks
az aks get-credentials -g ngsa-pre-west-app-rg -n ngsa-pre-west-aks

# check contexts exist
kubectl config get-contexts

```

### Create Aliases

```bash

# optional but saves a LOT of typing

# main kubectl alias
alias k='kubectl'

# get all
alias kall='kubectl get all --all-namespaces'

# show contexts from kube config
alias kcg='kubectl config get-contexts'

# use your AKDC context (if exists)
alias kcl='kubectl config use-context kubernetes-admin@kubernetes'

# use one of the NGSA AKS contexts
alias kcc='kubectl config use-context ngsa-pre-central-aks'
alias kce='kubectl config use-context ngsa-pre-east-aks'
alias kcw='kubectl config use-context ngsa-pre-west-aks'

```

### Start a load test(s)

- For this example we use the `load-10.yaml` which generates approximately 10 additional req/sec
- You can apply to one or more clusters

```bash
# select your cluster (west)
kcw

# create the loderunner pod
k apply -f load-10.yaml

# check the logs
k logs l8r-load-10

# central
kcc
k apply -f load-10.yaml
k logs l8r-load-10

# east
kce
k apply -f load-10.yaml
k logs l8r-load-10

```

### Stop the load tests(s)

```bash

# select your cluster
kcw

# delete the loderunner pod
k delete -f load-10.yaml

# central
kcc
k delete -f load-10.yaml

# east
kce
k delete -f load-10.yaml

```
