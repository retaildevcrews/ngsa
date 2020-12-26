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

# check local logs
kubectl get pods

# get the Cluster IP address
export ngsa_memory="http://$(kubectl get svc ngsa-memory | awk '{print $3}' | tail -n 1):4120"

# curl the IP addresses of the cluster IP to validate service
curl $ngsa_memory/version

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# check local logs
kubectl logs baseline-memory

# delete baseline
kubectl delete -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# deploy LodeRunner
kubectl apply -f ../loderunner/benchmark-memory.yaml

# check pods
kubectl get pods

# check logs
kubectl logs benchmark-memory

# delete LodeRunner
kubectl delete -f ../loderunner/benchmark-memory.yaml

# delete ngsa-memory
kubectl delete -f in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
