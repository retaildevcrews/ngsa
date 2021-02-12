# Setup Azure Kubernetes Dev Cluster

> This will setup a single node k8s development cluster on an Azure VM

## Create Azure VM

Create your Azure VM per instructions at [Azure Kubernetes Development Cluster](https://github.com/retaildevcrews/akdc)

> From a Dev Cluster bash shell via SSH

```bash

# clone this repository
git clone https://github.com/retaildevcrews/ngsa

# change to this directory
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

- Follow the deployment instructions in [ngsa-memory](ngsa-memory/README.md)

### Deploy LodeRunner

> This requires NGSA-Memory

```bash

kubectl apply -f loderunner/loderunner.yaml

```

### Deploy NGSA-Cosmos

- Follow the deployment instructions in [ngsa-cosmos](ngsa-cosmos/README.md)

### Setup Prometheus

```bash

# create the volume mount directory
sudo mkdir -p /prometheus    
sudo chown -R 65534:65534 /prometheus

kubectl apply -f prometheus

```

### Setup Grafana

- Default Credentials
  - user: ngsa
  - password: Ngsa512

```bash

# create the volume mount directory
sudo cp -R grafanadata /grafana
sudo chown -R 472:472 /grafana

kubectl apply -f grafana

```

### Setup kube state metrics

```bash

kubectl apply -f kube-state-metrics

```

### Setup  Fluent Bit

- Follow the deployment instructions in [fluentbit](fluentbit/README.md)
