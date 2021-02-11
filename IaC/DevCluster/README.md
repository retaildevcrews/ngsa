# Setup Azure Kubernetes Dev Cluster

> This will setup a single node k8s development cluster on an Azure VM

## Create Azure VM

Create your Azure VM per instructions at [Azure Kubernetes Development Cluster](https://github.com/retaildevcrews/akdc)

> From an Azure VM bash shell via SSH

```bash

# clone this repository
git clone https://github.com/retaildevcrews/ngsa

# change to the correct directory
cd ngsa/IaC/DevCluster

# verify kubernetes is running
kubectl get all --all-namespaces

```

### Open firewall ports

> (optional) Allows remote access (via http) to your Dev Cluster

- Prometheus web is on port 30000
- NGSA-Memory is on port 30080
- NGSA-Cosmos is on port 30081
- LodeRunner is on port 30088
- Grafana web is on port 32000

### Deploy NGSA-Memory

Follow the deployment instructions in [ngsa-memory](ngsa-memory/README.md)

### Deploy NGSA-Cosmos

Follow the deployment instructions in [ngsa-cosmos](ngsa-cosmos/README.md)

### Setup Prometheus

```bash

kubectl apply -f prometheus

```

### Setup Grafana

- Import dashboard
  - dashboards/dotnet.json
  - dashboards/ngsa.json

```bash

kubectl apply -f kube-state-metrics

```

### Setup kube state metrics

```bash

kubectl apply -f kube-state-metrics

```

### Setup  Fluent Bit

Follow the deployment instructions in [fluentbit](fluentbit/README.md) to setup Fluent Bit and Azure Log Analytics
