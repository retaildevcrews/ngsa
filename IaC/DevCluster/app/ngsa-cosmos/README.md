# App Setup for Cosmos DB

## Create Cosmos DB

Follow the instructions [here](https://github.com/retaildevcrews/imdb) to create and load the IMDb data into Cosmos DB

## Create app secrets

- Make sure the following environment variables are set correction
  - Imdb_DB
  - Imdb_Col
  - Imdb_Name
  - Imdb_RG

```bash

# create app secrets
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosDatabase=$Imdb_DB \
  --from-literal=CosmosCollection=$Imdb_Col \
  --from-literal=CosmosKey=$(az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryReadonlyMasterKey -o tsv) \
  --from-literal=CosmosUrl=https://${Imdb_Name}.documents.azure.com:443/

# display the secrets (base 64 encoded)
kubectl get secret ngsa-secrets -o jsonpath='{.data}'

```

## Deploy ngsa-cosmos

```bash

# set temporary Log Analytics secrets (if necessary)
kubectl create secret generic log-secrets \
  --from-literal=WorkspaceId=dev \
  --from-literal=SharedKey=dev

# deploy ngsa app
kubectl apply -f ngsa.yaml

# check pods until running
kubectl get pods

# check local logs
kubectl logs ngsa-cosmos -c app

# save the cluster IP
export ngsa=http://$(kubectl get service ngsa-cosmos -o jsonpath="{.spec.clusterIP}"):4120

# check the version and genres endpoints
http $ngsa/version
http $ngsa/api/genres

# check logs
kubectl logs ngsa-cosmos -c app

# delete ngsa app
kubectl delete -f ngsa.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
