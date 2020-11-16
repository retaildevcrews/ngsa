# Debugging fluentd

Debugging fluentd on a local cluster by sending everything to fluentd stdout

```bash

# start in this directory
cd fluentd/debug

# create the service account
kubectl apply -f role-fluentd-debug.yaml

# create app configmap
kubectl apply -f zone-config-debug.yaml

# deploy ngsa-memory
kubectl apply -f in-memory-debug.yaml

#### wait for ngsa to start

# run webv with baseline.json
kubectl apply -f baseline-debug.yaml

#### the baseline pod will show as complete quickly
#### do not delete the pod

### change these values
# set env vars
export LOG_ID=YourWorkspaceID
export LOG_SHARED_KEY=YourSharedKey

# apply the config and create fluentd pod
./apply-config

# check fluentd logs for baseline entries
# this may take 10 seconds
kubectl logs fluentd

# delete fluentd
# this deletes the fluentd pod
# this deletes the fluentd position file so that you can test the same logs
./delete-fluentd

# leave ngsa and baseline pods running
# deleting a pod also deletes it's log files

```

## Test sending to Log Analytics

```bash

# create app pods (if necessary)
kubectl apply -f in-memory.yaml
kubectl apply -f baseline.yaml

### leave both pods running

# this will create Log Analytics custom logs of the form:
#  webvsuffix
#  ngsasuffix
#  kubesuffix
#  suffix can ONLY be a-z; no punctuation or numeric values
# leave unset to use webv, ngsa and kube log names
export LOG_SUFFIX=a

# edit config-debug.yaml
# uncomment log analytics lines
# comment @type stdout line

# update config and start fluentd
./apply-config

# check Log Analytics for your data
# this can take 10-15 minutes :(

# delete the app
kubectl delete -f baseline-debug.yaml
kubectl delete -f fluentd-debug.yaml
kubectl delete -f in-memory-debug.yaml

# delete configmaps and role (not necessary)
kubectl delete -f config-fluentd-debug.yaml
kubectl delete -f role-fluentd-debug.yaml
kubectl delete -f zone-config-debug.yaml

```
