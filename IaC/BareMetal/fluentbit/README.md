# Debugging Fluent Bit

Debugging Fluent Bit on a local dev cluster by sending everything to stdout and then to Azure Log Analytics

```bash

# start in the fluentbit directory
cd fluentbit

### Create secrets if necessary
### fluentbit won't run without these secrets
### skip this step if already set
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=unused \
  --from-literal=SharedKey=unused

# create the fluentbit service account
kubectl apply -f account.yaml

# apply fluentbit config to log to stdout
kubectl apply -f debug-stdout.yaml

# create configmap
kubectl apply -f ../app/config.yaml

# deploy ngsa-memory
kubectl apply -f ../app/in-memory.yaml

# check pods
kubectl get pods

# wait for app to start
# Now listening on: http://[::]:4120
# Application started. Press Ctrl+C to shut down.
kubectl logs ngsa-memory

# start fluentbit pod
kubectl apply -f debug-pod.yaml

# check pods
kubectl get pods

# check the logs
kubectl logs fluentb

# save the cluster IP
export ngsa=$(kubectl get service | grep ngsa | awk '{print $3}'):4120

# check the version and genres endpoints
http http://$ngsa/version
http http://$ngsa/api/genres

# check the logs again
kubectl logs fluentb

# delete fluentb
kubectl delete -f debug-pod.yaml

# delete ngsa-memory
kubectl delete -f ../app/in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```

## Test sending to Log Analytics

```bash

# create app pod
kubectl apply -f ../app/in-memory.yaml

# apply the config and create fluentb pod
# todo - test this
kubectl apply -f debug-loga.yaml

# start fluentbit pod
kubectl apply -f debug-pod.yaml

# check pods
kubectl get pods

# check fluentb logs
kubectl logs fluentb

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# delete baseline test after status: Completed
kubectl delete -f ../loderunner/baseline-memory.yaml

# check pods
kubectl get pods

# check fluentb logs
kubectl logs fluentb

# looking for a line like:
#   [2020/11/16 21:54:19] [ info] [output:azure:azure.*]

# check Log Analytics for your data
# this can take 10-15 minutes :(

# delete the app
kubectl delete -f debug-pod.yaml
kubectl delete -f ../app/in-memory.yaml

# check pods
kubectl get pods

# Result - No resources found in default namespace.

```
