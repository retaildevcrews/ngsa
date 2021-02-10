# Prometheus Setup

Instructions for setting up prometheus monitoring on Dev Cluster

- Apply the yaml in order
- Open your VM firewall rule(s) to access remotely
  - Prometheus web is on port 30000
  - Grafana web is on port 32000
  - NGSA-App is on port 30080
  - LodeRunner is on port 30088

```bash

kubectl apply -f 1-clusterRole.yaml
kubectl apply -f 2-config-map.yaml
kubectl apply -f 3-prometheus-deployment.yaml
kubectl apply -f 4-prometheus-service.yaml

# deploy kube state metrics (optional)
kubectl apply -f kube-state-metrics

# deploy grafan (optional)
kubectl apply -f grafana

```

You can find the full tutorial from [here](https://devopscube.com/setup-prometheus-monitoring-on-kubernetes/)
