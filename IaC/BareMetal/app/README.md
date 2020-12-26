# App Setup

```bash

# Set app secrets

# delete if necessary - you can safely ignore the not exists error
kubectl delete secret ngsa-secrets

# if you aren't using Cosmos or Log Analytics
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=dev \
  --from-literal=SharedKey=dev

# Add Cosmos and Log Analytics values from Azure
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

# add configmap to cluster
kubectl apply -f config.yaml

# deploy ngsa-memory
kubectl apply -f in-memory.yaml

# check pods
kubectl get pods

# check local logs
kubectl logs baseline-memory

# save the cluster IP
export ngsa=$(kubectl get service | grep ngsa | awk '{print $3}'):4120

# check the version and genres endpoints
http http://$ngsa/version
http http://$ngsa/api/genres

# check local logs
kubectl logs baseline-memory

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

# check local logs
kubectl logs baseline-memory

# delete baseline
kubectl delete -f ../loderunner/baseline-memory.yaml

# delete ngsa-memory
kubectl delete -f in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
