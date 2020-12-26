# Debugging Fluent Bit

Debugging Fluent Bit on a local cluster by sending everything to stdout and then to Azure Log Analytics

```bash

# start in this directory
cd fluentbit

### Create secrets only if skipped
### fluentbit won't run without these secrets set
kubectl create secret generic ngsa-secrets \
  --from-literal=WorkspaceId=unused \
  --from-literal=SharedKey=unused

# create the service account
kubectl apply -f account.yaml

# create configmap
kubectl apply -f ../app/config.yaml

# deploy ngsa-memory
kubectl apply -f ../app/in-memory.yaml

#### wait for ngsa to start

# apply fluentbit to log to stdout
kubectl apply -f debug-stdout.yaml

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
kubectl apply -f ../app/in-memory.yaml

# apply the config and create fluentb pod
kubectl apply -f debug-loga.yaml

# check fluentb logs
kubectl logs fluentb

# run baseline test
kubectl apply -f ../loderunner/baseline-memory.yaml

### leave both pods running

# check fluentb logs
# looking for a line like:
#   [2020/11/16 21:54:19] [ info] [output:azure:azure.*]

# check Log Analytics for your data
# this can take 10-15 minutes :(

# delete the app
kubectl delete -f ../loderunner/baseline-memory.yaml
kubectl delete -f fluentbit-debug.yaml
kubectl delete -f ../app/in-memory.yaml

# delete configmaps and role (not necessary)
kubectl delete -f config.yaml
kubectl delete -f account.yaml
kubectl delete -f ../app/config.yaml

```
