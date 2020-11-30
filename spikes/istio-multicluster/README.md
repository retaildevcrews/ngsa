# Multi-cluster mesh, shared control plane

Example setup of an Istio service mesh with 2 AKS clusters and load balancing across both clusters. This setup will use an ingress-gateway on each cluster to direct traffic.

<https://istio.io/v1.7/docs/setup/install/multicluster/shared/>

## Prerequisites

- Istio CLI 1.7.3 ([download instructions](https://istio.io/latest/docs/setup/getting-started/#download))
- Azure CLI ([download instructions](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))

## Setup

```bash

# Clone the repository
git clone git@github.com:retaildevcrews/ngsa.git

# cd into the multi-cluster spikes directory
cd ngsa/spikes/istio-multicluster

# Set variables
AKS_VERSION="1.18.8"
LOCATION="centralus"
RESOURCE_GROUP="rg-istiodemo-$LOCATION"
MAIN_CLUSTER_CTX="aks-istiodemo-main-$LOCATION"
REMOTE_CLUSTER_CTX="aks-istiodemo-remote0-$LOCATION"

```

Create a resource group

```bash

az group create --name $RESOURCE_GROUP --location $LOCATION

```

Create 2 AKS clusters. The main cluster will have the Istio control plane components.

```bash

# Create the main cluster
az aks create \
  --name $MAIN_CLUSTER_CTX \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-cluster-autoscaler --min-count 1 --max-count 3 \
  --node-count 3 \
  --kubernetes-version $AKS_VERSION \
  --no-ssh-key \
  --enable-managed-identity

# Create the remote cluster
az aks create \
  --name $REMOTE_CLUSTER_CTX \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-cluster-autoscaler --min-count 1 --max-count 3 \
  --node-count 3 \
  --kubernetes-version $AKS_VERSION \
  --no-ssh-key \
  --enable-managed-identity

az aks get-credentials --resource-group $RESOURCE_GROUP --name $MAIN_CLUSTER_CTX
az aks get-credentials --resource-group $RESOURCE_GROUP --name $REMOTE_CLUSTER_CTX

```

## Certificate Authority

> **The root and intermediate certificate from the samples directory are widely distributed and known. Do not use these certificates in production as your clusters would then be open to security vulnerabilities and compromise.**

Setup the certificates and keys needed to enable mutual TLS communication between the 2 clusters. This demo will be using the sample certificates and keys provided by Istio for demo purposes. Do not use these in production. Download these four sample files from <https://github.com/istio/istio/tree/1.7.3/samples/certs>.

- ca-cert.pem
- ca-key.pem
- root-cert.pem
- cert-chain.pem

Create secrets in each cluster referencing the sample certs wherever they were saved.

```bash

# Main cluster
kubectl create namespace istio-system --context $MAIN_CLUSTER_CTX
kubectl create secret generic testing -n istio-system --context $MAIN_CLUSTER_CTX \
    --from-file=/tmp/ca-cert.pem \
    --from-file=/tmp/ca-key.pem \
    --from-file=/tmp/root-cert.pem \
    --from-file=/tmp/cert-chain.pem

# Remote cluster
kubectl create namespace istio-system --context $REMOTE_CLUSTER_CTX
kubectl create secret generic cacerts -n istio-system --context $REMOTE_CLUSTER_CTX \
    --from-file=/tmp/ca-cert.pem \
    --from-file=/tmp/ca-key.pem \
    --from-file=/tmp/root-cert.pem \
    --from-file=/tmp/cert-chain.pem

```

## Cluster and network naming

Name the clusters and networks for the mesh. These values will be used to configure the Istio operator in each cluster.

```bash

# The name of the clusters from the perspective of the mesh
MAIN_CLUSTER_NAME=main0
REMOTE_CLUSTER_NAME=remote0

# The name of the networks the clusters are on from the perspective of the mesh.
# For this walkthrough, the assumption is that the clusters are on 2 different networks.
MAIN_CLUSTER_NETWORK=network1
REMOTE_CLUSTER_NETWORK=network2

```

## Install Istio and ingress gateways

Create the primary cluster configuration for using the ingress-gateways to do cross-cluster communication. Specifiy the network information for each cluster in the mesh in the configuration for the main cluster.

```bash

cat <<EOF> istio-main-cluster.yaml
apiVersion: install.istio.io/v1alpha1
kind: IstioOperator
spec:
  values:
    global:
      multiCluster:
        clusterName: ${MAIN_CLUSTER_NAME}
      network: ${MAIN_CLUSTER_NETWORK}

      # Mesh network configuration. This is optional and may be omitted if
      # all clusters are on the same network.
      meshNetworks:
        ${MAIN_CLUSTER_NETWORK}:
          endpoints:
          - fromRegistry:  ${MAIN_CLUSTER_NAME}
          gateways:
          - registry_service_name: istio-ingressgateway.istio-system.svc.cluster.local
            port: 443

        ${REMOTE_CLUSTER_NETWORK}:
          endpoints:
          - fromRegistry: ${REMOTE_CLUSTER_NAME}
          gateways:
          - registry_service_name: istio-ingressgateway.istio-system.svc.cluster.local
            port: 443

      # Use the existing istio-ingressgateway.
      meshExpansion:
        enabled: true
EOF

# Apply the primary cluster's configuration
istioctl install -f istio-main-cluster.yaml --context $MAIN_CLUSTER_CTX

# Wait for the pods to be ready before continuing
kubectl get pod -n istio-system --context $MAIN_CLUSTER_CTX

```

Get the IP address of the ingress gateway load balancer on the Main cluster. This will be used when configuring the remote cluster.

```bash

ISTIOD_REMOTE_EP=$(kubectl get svc -n istio-system --context $MAIN_CLUSTER_CTX istio-ingressgateway -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

```

Create the remote cluster configuration using the ingress-gateways to do cross-cluster communication. Part of the configuration is to tell the remote cluster the IP address of the ingress gateway in the main cluster.

```bash

cat <<EOF> istio-remote0-cluster.yaml
apiVersion: install.istio.io/v1alpha1
kind: IstioOperator
spec:
  values:
    global:
      # The remote cluster's name and network name must match the values specified in the
      # mesh network configuration of the primary cluster.
      multiCluster:
        clusterName: ${REMOTE_CLUSTER_NAME}
      network: ${REMOTE_CLUSTER_NETWORK}

      # Replace ISTIOD_REMOTE_EP with the the value of ISTIOD_REMOTE_EP set earlier.
      remotePilotAddress: ${ISTIOD_REMOTE_EP}
EOF

# Apply the primary cluster's configuration
istioctl install -f istio-remote0-cluster.yaml --context ${REMOTE_CLUSTER_CTX}

# Wait for the pods to be ready before continuing
kubectl get pod -n istio-system --context $REMOTE_CLUSTER_CTX

```

## Cross-cluster load load balancing

> Cross-network traffic is securely routed through each destination cluster’s ingress gateway.

The gateway in each cluster will allow "local" traffic to pass through the mesh. Even though the traffic is comming from another cluster, potentially across the public internet, allow it to enter the mesh. The certificates that were setup earlier in each cluster will be used for mTLS communication between the clusters.

```bash

kubectl apply -f samples/cluster-aware-gateway.yaml --context $MAIN_CLUSTER_CTX
kubectl apply -f samples/cluster-aware-gateway.yaml --context $REMOTE_CLUSTER_CTX

```

> To enable cross-cluster load balancing, the Istio control plane requires access to all clusters in the mesh to discover services, endpoints, and pod attributes.

Give the main cluster access to the remote cluster's kube-apiserver.

```bash

istioctl x create-remote-secret --name ${REMOTE_CLUSTER_NAME} --context $REMOTE_CLUSTER_CTX | \
  kubectl apply -f - --context $MAIN_CLUSTER_CTX

# You should now see a secret in the main cluster with a kubeconfig for accessing the remote cluster.
kubectl get secrets istio-remote-secret-remote0 -n istio-system --context $MAIN_CLUSTER_CTX -o jsonpath="{.data.remote0}" | base64 -d

```

## Testing deployment with example app

This demo will use the sample helloworld app that is provided by Istio.

```bash

# Create a namespace for the deployment in the main cluster
kubectl create namespace sample --context $MAIN_CLUSTER_CTX
# Add the namespace to the service mesh
kubectl label namespace sample istio-injection=enabled --context $MAIN_CLUSTER_CTX

# Deploy v1 of the app in the main cluster
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/helloworld/helloworld.yaml -l app=helloworld -n sample --context $MAIN_CLUSTER_CTX
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/helloworld/helloworld.yaml -l version=v1 -n sample --context $MAIN_CLUSTER_CTX

# Confirm that the pods are running
kubectl get pod -n sample --context $MAIN_CLUSTER_CTX

# Create a namespace for the deployment in the remote cluster
kubectl create namespace sample --context $REMOTE_CLUSTER_CTX
# Add the namespace to the service mesh
kubectl label namespace sample istio-injection=enabled --context $REMOTE_CLUSTER_CTX

# Deploy v2 of the app in the main cluster
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/helloworld/helloworld.yaml -l app=helloworld -n sample --context $REMOTE_CLUSTER_CTX
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/helloworld/helloworld.yaml -l version=v2 -n sample --context $REMOTE_CLUSTER_CTX

# Confirm that the pods are running
kubectl get pod -n sample --context $REMOTE_CLUSTER_CTX

# Deploy sleep pods in each cluster to use for testing. These pods will be used to send traffic to the helloworld service and also view some Istio configs.
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/sleep/sleep.yaml -n sample --context $MAIN_CLUSTER_CTX

kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/sleep/sleep.yaml -n sample --context $REMOTE_CLUSTER_CTX

```

Test the cross-cluster load balancing

```bash

# Find a sleep pod in the main cluster
kubectl get pods -n sample -l app=sleep --context $MAIN_CLUSTER_CTX

# Exec into a sleep container
# Call the hello world service several times from inside the container. You should eventually see responses from both v1 and v2.
kubectl exec -it <podname> -c sleep --context $MAIN_CLUSTER_CTX -n sample -- curl helloworld.sample:5000/hello

# You can also use the watch command to send continuous traffic
kubectl exec -it <podname> -c sleep --context $MAIN_CLUSTER_CTX -n sample -- watch curl helloworld.sample:5000/hello

# Follow the same process on the remote cluster. The behavior should be be same with responses from v1 and v2.

# Find a sleep pod in the remote cluster
kubectl get pods -n sample -l app=sleep --context $REMOTE_CLUSTER_CTX

# Exec into a sleep container
# Call the hello world service several times from inside the container. You should eventually see responses from both v1 and v2.
kubectl exec -it <podname> -c sleep --context $REMOTE_CLUSTER_CTX -n sample -- curl helloworld.sample:5000/hello

# You can also use the watch command to send continuous traffic
kubectl exec -it <podname> -c sleep --context $REMOTE_CLUSTER_CTX -n sample -- watch curl helloworld.sample:5000/hello

```

## Istio configs

You can view the endpoints of the helloworld app from the perspective of each cluster in the service mesh.

```bash

# Find a helloworld pod in the main cluster
kubectl get pods -n sample -l app=helloworld --context $MAIN_CLUSTER_CTX

# Search for the helloworld endpoints
istioctl -n sample --context $MAIN_CLUSTER_CTX proxy-config endpoints <podname> --cluster "outbound|5000||helloworld.sample.svc.cluster.local"

# You should now see 2 entries.

# The app in the current cluster has the internal IP address of the helloworld pod
# "10.244.1.9:5000      HEALTHY     OK                outbound|5000||helloworld.sample.svc.cluster.local"

# The app in the remote cluster has the IP address of the ingress gateway on the remote cluster
# "13.86.57.22:443      HEALTHY     OK                outbound|5000||helloworld.sample.svc.cluster.local"

# You can verify this by checking the IP address of the helloworld pod in the main cluster.
kubectl get pods -o wide -n sample -l app=helloworld --context $MAIN_CLUSTER_CTX

# Also check the external IP address of the ingress gateway on the remote cluster.
kubectl get service istio-ingressgateway -n istio-system --context $REMOTE_CLUSTER_CTX

# These should match their respective values from the Istio endpoint entries

```

Scale the helloworld deployment in the main cluster to have more than 1 pod. Then check the istio endpoints again. You should see entries with internal IP addresses for the new pods.

```bash

# Scale the helloworld deployment in the main cluster
kubectl scale deployment helloworld-v1 --replicas=2 -n sample --context $MAIN_CLUSTER_CTX

# When the new pods are ready, check the endpoints again.
istioctl -n sample --context $MAIN_CLUSTER_CTX proxy-config endpoints <podname> --cluster "outbound|5000||helloworld.sample.svc.cluster.local"

# There should now be mutlple entries with internal addresses and still 1 entry with an external address.

```

Now scale the helloworld deployment in the remote cluster to have more than 1 pod. Compare the endpoints in the main cluster and the remote cluster.

```bash

# Scale the helloworld deployment in the remote cluster
kubectl scale deployment helloworld-v2 --replicas=2 -n sample --context $REMOTE_CLUSTER_CTX

# Find a helloworld pod in the remote cluster
kubectl get pods -n sample -l app=helloworld --context $REMOTE_CLUSTER_CTX

# When the new pods are ready, check the endpoints.
istioctl -n sample --context $REMOTE_CLUSTER_CTX proxy-config endpoints <remote-cluster-podname> --cluster "outbound|5000||helloworld.sample.svc.cluster.local"

# This time in the remote cluster, you should see multiple entries with internal address and another with en external address for the main cluster ingress gateway

# Internal address for local endpoints in the remote cluster
# 10.244.0.15:5000     HEALTHY     OK                outbound|5000||helloworld.sample.svc.cluster.local
# 10.244.0.9:5000      HEALTHY     OK                outbound|5000||helloworld.sample.svc.cluster.local

# External address for the ingress gateway in the main cluster
# 52.191.34.44:443     HEALTHY     OK                outbound|5000||helloworld.sample.svc.cluster.local

# Scale the deployment down again.
kubectl scale deployment helloworld-v1 --replicas=1 -n sample --context $MAIN_CLUSTER_CTX
kubectl scale deployment helloworld-v2 --replicas=1 -n sample --context $REMOTE_CLUSTER_CTX

```

Though each cluster has multiple pods for the helloworld app, each cluster still only has 1 entry for the other cluster. Which is the ingress gateway where cross cluster traffic is routed.

## Traffic management and UI

Deploy the kiali and prometheus addon to visualize the mesh.

```bash

# Deploy prometheus
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/addons/prometheus.yaml -n istio-system --context $MAIN_CLUSTER_CTX

```

If you run into an error the first time applying the kiali yaml, `unable to recognize ... in version "monitoring.kiali.io/v1alpha1"`, apply the kiali yaml again.

```bash

# Deploy kiali
kubectl apply -f https://raw.githubusercontent.com/istio/istio/1.7.3/samples/addons/kiali.yaml -n istio-system --context $MAIN_CLUSTER_CTX

# Start the kiali UI

# Run this in a different terminal
istioctl dashboard kiali --context $MAIN_CLUSTER_CTX

# Open a web browser and go to "http://localhost:20001/kiali"

```

Add a virtual service and a destination rule to the main cluster. These will be used to controll the helloworld traffic. The sample virtual service has weights for each version of the application. The destination rule creates subsets that map to each version of the app. The virtual service will reference these subsets when specifying the weights for the traffic flow between the helloworld apps.

```bash

# Apply the destination rule to the main cluster
kubectl apply -f samples/helloworld/destinationrule.yaml -n sample --context $MAIN_CLUSTER_CTX

# Apply the virtual service to the main cluster
kubectl apply -f samples/helloworld/virtualservice.yaml -n sample --context $MAIN_CLUSTER_CTX

```

Go to the kiali dashboard and select "Graph" in the sidebar. At the top of the graph select these filters. For "Namespace", select "sample". Next to the "Namespace" filter, change the visualization to "Versioned app graph". For the "Display" filter, choose "Requests Percentage". You can also select any of the other "Show" and "Show Badges" options.

Send continous traffic to the hellowrold app and observe the traffic percentage in the graph.

```bash

# Find a sleep pod
kubectl get pods -n sample -l app=sleep --context $MAIN_CLUSTER_CTX

# Send constant traffic to the helloworld service
kubectl exec -it <podname> -c sleep --context $MAIN_CLUSTER_CTX -n sample -- watch curl helloworld.sample:5000/hello

```

Observe the graph and the traffic percentage should be close to a 50/50 split. It won't be exact. Now change the weights in samples/helloworld/virtualservice.yaml to something extreme. Like a 90/10 split. The total be equal to 100.

Apply the updated virtual service to the main cluster, run the command to send continous traffic aggain, and observe the graph.

```bash

# Apply the updated virtual service to the main cluster
kubectl apply -f samples/helloworld/virtualservice.yaml -n sample --context $MAIN_CLUSTER_CTX

# Send constant traffic to the helloworld service and observe the updated behavior
kubectl exec -it <podname> -c sleep --context $MAIN_CLUSTER_CTX -n sample -- watch curl helloworld.sample:5000/hello

```
