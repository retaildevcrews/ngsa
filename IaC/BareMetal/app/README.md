# App Setup

```bash

# Set app secrets

# delete if necessary - you can safely ignore the not exists error
kubectl delete secret ngsa-secrets

# if you aren't using Cosmos or Log Analytics
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=dev \
  --from-literal=SharedKey=dev

### TODO - I think we should move this to a separate section
##         focus the initial quick start on in-memory + stdout only


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
### TODO - update a real secret
kubectl create secret generic ngsa-secrets \
  --from-literal=foo=bar \
  --dry-run=client -o yaml | kubectl apply -f -

# add app configmap to cluster
kubectl apply -f config.yaml

# deploy ngsa-memory app
kubectl apply -f in-memory.yaml

# check pods until running
kubectl get pods

# check local logs
kubectl logs baseline-memory

# save the cluster IP
export ngsa=http://$(kubectl get service | grep ngsa-memory | awk '{print $3}'):4120

# check the version and genres endpoints
http $ngsa/version
http $ngsa/api/genres

# check local logs
kubectl logs ngsa-memory

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# check local logs
kubectl logs baseline-memory

# delete baseline after status is Completed
kubectl delete -f ../loderunner/baseline-memory.yaml

# setup port forwarding
# this allows you to access the ngsa-memory service
# from your local browser
kubectl port-forward svc/ngsa-memory 4120:4120

# open your local browser
http://127.0.0.1:4120/version
http://127.0.0.1:4120/api/genres

# press ctl-c to stop port forwarding

# delete ngsa-memory
kubectl delete -f in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
