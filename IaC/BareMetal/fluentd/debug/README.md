# Debugging fluentd

Debugging fluentd on a local cluster by sending everything to fluentd stdout

```bash

# create the service account
kubectl apply -f role.yaml

# deploy ngsa-memory
kubectl apply -f in-memory.yaml

### change these values
# set env vars
export LOG_ID=YourWorkspaceID
export LOG_SHARED_KEY=YourSharedKey

# apply the config
envsubst < config-debug.yaml | kubectl apply -f -

# start fluentd pod
kubectl apply -f fluentd-debug.yaml

### easier way to do both
./apply-config

# run webv with baseline.json
kubectl apply -f baseline.yaml

#### the baseline pod will show as complete quickly
#### do not delete the pod yet

# check fluentd logs for baseline entries
# this may take 10 seconds
kubectl logs fluentd

# delete baseline pod
kubectl delete pod baseline

# delete fluentd pod
kubectl delete pod fluentd

# you can leave ngsa pod running

```

## Test sending to Log Analytics

```bash

# create ngsa pod (if necessary)
kubectl apply -f in-memory.yaml

# edit config-debug.yaml
# uncomment log analytics lines

# update config
./apply-config

# start fluentd
kubectl apply -f fluentd-debug.yaml

# run baseline
kubectl apply -f baseline.yaml

### leave baseline running

# check Log Analytics for your data

# delete everything
kubectl delete -f baseline.yaml
kubectl delete -f fluentd-debug.yaml
kubectl delete -f in-memory.yaml

```
