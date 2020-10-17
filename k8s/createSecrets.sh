#!/bin/bash

# delete if necessary
kubectl delete secret ngsa-secrets

# create from key vault
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosDatabase) \
  --from-literal=CosmosCollection=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosCollection) \
  --from-literal=CosmosKey=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosKey) \
  --from-literal=CosmosUrl=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name CosmosUrl) \
  --from-literal=AppInsightsKey=$(eval az keyvault secret show --vault-name ngsa --query value -o tsv --name AppInsightsKey)

# display the secrets
kubectl get secret ngsa-secrets -o jsonpath='{.data}'
