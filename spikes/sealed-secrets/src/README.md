## Overview

This is a spike to get familiar with sealed secrets and see how they can be applied on NGSA project.

For details, visit ["Sealed Secrets" for Kubernetes](https://github.com/bitnami-labs/sealed-secrets).

## Setup sealed secrets in a cluster

1. Install sealed secrets in cluster with Helm
```

helm repo add sealed-secrets https://bitnami-labs.github.io/sealed-secrets
helm install sealed-secrets --namespace kube-system --version 1.13.2 sealed-secrets/sealed-secrets

```

2. Install sealed secrets CLI tool 
 
- on macos
```
brew install kubeseal
```

- on linux 
```
wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.13.1/kubeseal-linux-amd64 -O kubesealsudo install -m 755 kubeseal /usr/local/bin/kubeseal
```
 

3. Create secret manifest, save as secret.aml
```yaml

apiVersion: v1
data:
  CosmosCollection: <CosmosCollection>
  CosmosDatabase: <CosmosDatabase>
  CosmosKey: <CosmosKey>
  CosmosUrl: <CosmosUrl>
kind: Secret
metadata:  
  name: ngsa-secrets
  namespace: ngsa
type: Opaque

```

4. Encrypt ``` secret.yaml ``` and save it as ``` sealedsecret.yaml ```.
```

kubeseal <secret.yaml >sealedsecret.yaml --controller-namespace kube-system --controller-name sealed-secrets --format yaml

```

5. Remove ``` secret.yaml ```.  
Push ```sealedsecret.yaml``` to repo that's being monitored by flux.


6. A few minutes later when flux sync is done, verify that the new secret is there. 
 
```

kubectl get secret ngsa-secrets -o yaml

```