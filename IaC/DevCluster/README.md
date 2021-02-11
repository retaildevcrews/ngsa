# Setup Azure Kubernetes Dev Cluster

> This will setup a single node k8s development cluster on an Azure VM

## Create Azure VM

Create your Azure VM per instructions at [Azure Kubernetes Development Cluster](https://github.com/retaildevcrews/akdc)

## SSH into the Azure VM

```bash

# AKDC_IP is set during the previous step
ssh akdc@${AKDC_IP}

```

### From the Azure VM

```bash

# clone this repository
git clone https://github.com/retaildevcrews/ngsa

# change to the correct directory
cd ngsa/IaC/DevCluster

# verify kubernetes is running
kubectl get all --all-namespaces

```

### Deploy the NGSA app

Follow the deployment instructions in [app](app/README.md) to deploy ngsa

- Open your VM firewall rule(s) to access remotely
  - NGSA-App is on port 30080
  - LodeRunner is on port 30088

### Deploy Fluent Bit (log forwarding)

Follow the deployment instructions in [fluentbit](fluentbit/README.md) to setup Fluent Bit and Azure Log Analytics

### Prometheus Setup

- Open your VM firewall rule(s) to access remotely
  - Prometheus web is on port 30000

```bash

kubectl apply -f prometheus

```

### Grafana Setup

- Open your VM firewall rule(s) to access remotely
  - Grafana web is on port 32000

```bash

kubectl apply -f kube-state-metrics

```

### kube state metrics setup


```bash

kubectl apply -f kube-state-metrics

```
