# Setup Azure Kubernetes Dev Cluster

> This will setup a single node k8s development cluster on an Azure VM

## Create Azure VM

Create your Azure VM per instructions at [Azure Kubernetes Development Cluster](https://github.com/retaildevcrews/akdc)

## SSH into the Azure VM

```bash

# AKDC_IP is set during the previous step
# the -L allows you to forward your port from the dev cluster via SSH tunneling
ssh -L 4120:127.0.0.1:4120 akdc@${AKDC_IP}

```

### From the Azure VM

```bash

# clone this repository
git clone https://github.com/retaildevcrews/ngsa

# change to the correct directory
cd ngsa/IaC/DevCluster/app

# verify kubernetes is running
kubectl get all --all-namespaces

```

### Deploy the NGSA app

Follow the deployment instructions in [app](app/README.md) to deploy ngsa

### Deploy Fluent Bit (log forwarding)

Follow the deployment instructions in [fluentbit](fluentbit/README.md) to setup Fluent Bit and Azure Log Analytics
