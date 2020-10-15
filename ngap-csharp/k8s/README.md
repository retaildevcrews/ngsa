# Set Secrets

Set ngap secrets from Azure Key Vault into k8s

```bash

# delete if necessary
kubectl delete secret ngap-secrets

# create from key vault
kubectl create secret generic ngap-secrets \
  --from-literal=CosmosDatabase=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosDatabase) \
  --from-literal=CosmosCollection=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosCollection) \
  --from-literal=CosmosKey=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosKey) \
  --from-literal=CosmosUrl=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name CosmosUrl) \
  --from-literal=AppInsightsKey=$(eval az keyvault secret show --vault-name ngap --query value -o tsv --name AppInsightsKey)

# display the secrets
k get secret ngap-secrets -o jsonpath='{.data}'

```

Deploy ngap-csharp

```bash

k apply -f ngap-csharp.yml

```

Check results

```bash

k get pods

k logs ngap-csharp

```
