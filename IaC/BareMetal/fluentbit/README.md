# Debugging Fluent Bit

Debugging Fluent Bit on a local dev cluster by sending everything to stdout and then to Azure Log Analytics

```bash

# start in the fluentbit directory
# assumes you were in the app directory
cd ../fluentbit

### Create secrets if necessary
###   fluentbit won't run without these secrets
###   skip this step if already set
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=unused \
  --from-literal=SharedKey=unused

# create the fluentbit service account
kubectl apply -f account.yaml

# apply fluentbit config to log to stdout
kubectl apply -f stdout-config.yaml

# create configmap
kubectl apply -f ../app/config.yaml

# deploy ngsa-memory
kubectl apply -f ../app/in-memory.yaml

# check pods
kubectl get pods

# wait for app to start
# Now listening on: http://[::]:4120
# Application started. Press Ctrl+C to shut down.
kubectl logs ngsa-memory

# start fluentbit pod
kubectl apply -f fluentbit-pod.yaml

# check pods
kubectl get pods

# check the logs
kubectl logs fluentb

# save the cluster IP
export ngsa=http://$(kubectl get service | grep ngsa-memory | awk '{print $3}'):4120

# check the version and genres endpoints
http $ngsa/version
http $ngsa/api/genres

# check the logs again
kubectl logs fluentb

# delete fluentb
kubectl delete -f fluentbit-pod.yaml

# delete ngsa-memory
kubectl delete -f ../app/in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```

## Test sending to Log Analytics

### Login to Azure

```bash

# login to Azure
az login

az account list -o table

# select subscription (if necesary)
az account set -s YourSubscriptionName

```

### Setup Log Analytics

```bash

# add az cli extension
az extension add --name log-analytics

# set environment variables
export AKDC_RG=akdc
export AKDC_LOC=westus2
export Ngsa_Log_Name=akdc

# create Log Analytics instance
az monitor log-analytics workspace create -g $AKDC_RG -n $Ngsa_Log_Name -l $AKDC_LOC

### TODO - in order for this to work, you have to install AZ CLI and login
###        alternatively, you could run kubectl from your local machine to the dev cluster

### TODO - I think we should move the LA create here

# delete ngsa-secrets
kubectl delete secret ngsa-secrets

# add Log Analytics secrets
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $AKDC_RG -n $Ngsa_Log_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $AKDC_RG -n $Ngsa_Log_Name --query primarySharedKey -o tsv)

# display the secrets (base 64 encoded)
kubectl get secret ngsa-secrets -o jsonpath='{.data}'

### TODO - I think Cosmos should be in a separate file

# add Cosmos and Log Analytics values from Azure if both are set
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/ \
  --from-literal=WorkspaceId=$(az monitor log-analytics workspace show -g $Ngsa_Log_RG -n $Ngsa_Log_Name --query customerId -o tsv) \
  --from-literal=SharedKey=$(az monitor log-analytics workspace get-shared-keys -g $Ngsa_Log_RG -n $Ngsa_Log_Name --query primarySharedKey -o tsv)
  
```

### Deploy to Kubernetes

```bash
# create app pod
kubectl apply -f ../app/in-memory.yaml

# apply the config and create fluentb pod
kubectl apply -f loga-config.yaml

# start fluentbit pod
kubectl apply -f fluentbit-pod.yaml

# check pods
kubectl get pods

# check fluentb logs
kubectl logs fluentb

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# delete baseline test after status: Completed
kubectl delete -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# check fluentb logs
kubectl logs fluentb

# looking for a line like:
#   [2020/11/16 21:54:19] [ info] [output:azure:azure.*]

# check Log Analytics for your data
# this can take 10-15 minutes :(

# delete the app
kubectl delete -f fluentbit-pod.yaml
kubectl delete -f ../app/in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
