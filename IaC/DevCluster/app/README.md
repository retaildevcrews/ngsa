# App Setup

```bash

# set temporary Log Analytics secrets
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=dev \
  --from-literal=SharedKey=dev

# display the secrets (base 64 encoded)
kubectl get secret ngsa-secrets -o jsonpath='{.data}'

# add app configmap to cluster
kubectl apply -f config.yaml

# deploy ngsa-memory app
kubectl apply -f in-memory.yaml

# check pods until running
kubectl get pods

# check local logs
kubectl logs ngsa-memory

# save the cluster IP
export ngsa=http://$(kubectl get service | grep ngsa-memory | awk '{print $3}'):4120

# check the version and genres endpoints
http $ngsa/version
http $ngsa/api/genres

# check logs
kubectl logs ngsa-memory

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
