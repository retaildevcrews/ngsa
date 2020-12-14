# App Setup

```bash

# add secrets

#### edit config.yaml for region / zone

# add configmap to cluster
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

# check local logs
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
