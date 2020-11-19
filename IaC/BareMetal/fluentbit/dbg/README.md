# Debugging Fluent Bit

Debugging Fluent Bit on a local cluster by sending everything to stdout and then to Azure Log Analytics

```bash

# start in this directory
cd fluentbit/dbg

# create the service account
kubectl apply -f role-fluentbit-debug.yaml

# create app configmap
kubectl apply -f zone-config-debug.yaml

# deploy ngsa-memory
kubectl apply -f in-memory-debug.yaml

#### wait for ngsa to start

# apply fluentbit to log to stdout
./apply-stdout

# check the logs
kubectl logs fluentb

# save the cluster IP
export ngsa=$(kubectl get service | grep ngsa | awk '{print $3}'):4120

# save to .bashrc (optional but handy)
echo "" >> ~/.bashrc
echo "export ngsa=$(kubectl get service | grep ngsa | awk '{print $3}'):4120" >> ~/.bashrc

# check the version endpoint
curl http://$ngsa/version

# check the version remotely

# if you are running kubectl on the bare metal VM, use SSH to forward your port
### from a new local terminal
ssh -L 4120:127.0.0.1:4120 YourIP-DNS

# setup port forwarding
kubectl port-forward svc/ngsa 4120:4120

# open your local browser
http://127.0.0.1:4120/version

# check the logs
kubectl logs fluentb

# delete fluentb
kubectl delete -f fluentbit-debug.yaml

# leave ngsa pod running
# deleting a pod also deletes it's log files

```

## Test sending to Log Analytics

```bash

# create app pod (if necessary)
kubectl apply -f in-memory.yaml

### change these values
# set env vars
export LOG_ID=YourWorkspaceID
export LOG_SHARED_KEY=YourSharedKey

# this will create Log Analytics custom logs of the form:
#  webvsuffix
#  ngsasuffix
#  kubesuffix
#  suffix can ONLY be a-z; no punctuation or numeric values
# leave unset to use webv, ngsa and kube log names
export LOG_SUFFIX=a

# apply the config and create fluentb pod
./apply-la

# check fluentb logs
kubectl logs fluentb

# run baseline test
kubectl apply -f baseline-debug.yaml

### leave both pods running

# check fluentb logs
# looking for a line like:
#   [2020/11/16 21:54:19] [ info] [output:azure:azure.*]

# check Log Analytics for your data
# this can take 10-15 minutes :(

# delete the app
kubectl delete -f baseline-debug.yaml
kubectl delete -f fluentbit-debug.yaml
kubectl delete -f in-memory-debug.yaml

# delete configmaps and role (not necessary)
kubectl delete -f la-config-debug.yaml
kubectl delete -f role-fluentbit-debug.yaml
kubectl delete -f zone-config-debug.yaml

```
