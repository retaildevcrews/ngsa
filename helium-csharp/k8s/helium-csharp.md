# Set Secrets

Set helium secrets from Azure Key Vault into k8s

```bash

# delete if necessary
kubectl delete secret helium-secrets

# create from key vault
kubectl create secret generic helium-secrets \
  --from-literal=CosmosDatabase=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosDatabase) \
  --from-literal=CosmosCollection=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosCollection) \
  --from-literal=CosmosKey=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosKey) \
  --from-literal=CosmosUrl=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosUrl) \
  --from-literal=AppInsightsKey=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name AppInsightsKey)

# display the secrets
k get secret helium-secrets -o jsonpath='{.data}'

```

Deploy helium-csharp

```bash

k apply -f helium-csharp.yml

```

Check results

```bash

k get pods

k logs helium-csharp

```
