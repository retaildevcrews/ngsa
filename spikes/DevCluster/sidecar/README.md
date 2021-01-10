# Deprecated

> This approach was a temporary solution until fluentbit was setup

## App Setup

With log agent sidecar

```bash

# add secrets

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

# add service account / role to cluster
kubectl apply -f role.yaml

# add configmap to cluster
#### edit config.yaml for region / zone
kubectl apply -f config.yaml

# deploy ngsa-cosmos
kubectl apply -f ngsa.yaml

# deploy ngsa-memory
kubectl apply -f in-memory.yaml

# check local logs
kubectl get all

# curl the IP addresses of both cluster IPs to validate service

curl 10.x.x.x:4120/version

# run baseline test
kubectl apply -f baseline.yaml

# check local logs and log analytics
kubectl delete -f baseline.yaml

# setup load balancer for ngsa-cosmos endpoint
# curl endpoint
curl https://ngsa-central.cse.ms/version
curl https://ngsa-east.cse.ms/version
curl https://ngsa-west.cse.ms/version

# after all 3 public endpoints are up and running
### this will fail if the public endpoints aren't up
### depending on final DNS naming, may need to update webv.yaml

# deploy webv
kubectl apply -f webv.yaml

# check local logs and log analytics

```
