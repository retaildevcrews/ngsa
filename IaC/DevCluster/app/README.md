# App Setup

```bash

# set temporary Log Analytics secrets
kubectl create secret generic log-secrets \
  --from-literal=WorkspaceId=dev \
  --from-literal=SharedKey=dev

# display the secrets (base 64 encoded)
kubectl get secret log-secrets -o jsonpath='{.data}'

# deploy ngsa-memory app
kubectl apply -f in-memory.yaml

# check pods until running
kubectl get pods

# check local logs
kubectl logs ngsa-memory

# check the version and genres endpoints
http localhost:30080/version
http localhost:30080/api/genres

# check logs
kubectl logs ngsa-memory

# delete ngsa-memory
kubectl delete -f in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
