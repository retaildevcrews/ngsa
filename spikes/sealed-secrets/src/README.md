## Overview

This is a spike to get familiar with sealed secrets and see how they can be applied on NGSA project.

## About Sealed Secrets
Sealed Secrets allow to manage all k8s config in git, <u>including Secrets</u>.

A Secret can be encrypted into a SealedSecret, which is safe to store - even to a public repository. The SealedSecret can be decrypted only by the controller running in the target cluster and nobody else (not even the original author) is able to obtain the original Secret from the SealedSecret.

For further details, visit ["Sealed Secrets" for Kubernetes](https://github.com/bitnami-labs/sealed-secrets).  

## Setup Sealed Secrets in a cluster

1. Install sealed secrets in a cluster with Helm
```

helm repo add sealed-secrets https://bitnami-labs.github.io/sealed-secrets
helm install sealed-secrets --namespace kube-system --version 1.13.2 sealed-secrets/sealed-secrets

```

2. Install sealed secrets CLI tool locally
 
- on macos
```
brew install kubeseal
```

- on linux 
```
wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.13.1/kubeseal-linux-amd64 -O kubesealsudo install -m 755 kubeseal /usr/local/bin/kubeseal
```
 

3. Create secret manifest, save as secret.yaml.  
<u><b>NOTE:</b> this file must not be shared and will be disposed off in later steps.</u>

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

NOTE: if the secret you want to check into git repo is already deployed to a cluster, you can retrive it with the following command:
```
k get secret -n ngsa ngsa-secrets -o yaml

```
Copy/paste it into a new yaml file and remove the unnecessary attributes leaving only those in the example above.

4. Encrypt ``` secret.yaml ``` and save it as ``` sealedsecret.yaml ```.
```

kubeseal <secret.yaml >sealedsecret.yaml --controller-namespace kube-system --controller-name sealed-secrets --format yaml

```

5. Remove ``` secret.yaml ```.  
Push ```sealedsecret.yaml``` to repo that's being monitored by flux.


6. Wait a few minutes for flux sync to happen or run the following command to force the sync:

```
fluxctl sync --k8s-fwd-ns fluxcd

```

Verify that the new secret is there. 
 
```

kubectl get secret ngsa-secrets -o yaml

```


## Setup sealed secrets rotation with gitops  

The sealed secrets controller and the associated workflow is designed to keep old sealing keys around and periodically add new ones.  
It is possible thought to manually manage (create, move, delete) sealing keys.  
Sealing keays are just normal k8s secrets living in the same namespace where the sealed secret controller lives. They therefore can be seales themselves, shared in a repo and used in a GitOps flow to be applied across multiple clusters.

<u><b>NOTE:</b> as of now, sealed secrets controller pod needs to be deleted each time sealing keys are updated so that the new keys could be picked up!</u>  

The following steps will:
- generate private sealing key
- encrypt private sealing key, and check it in to laverage GitOps flow for sharing among multiple clusters and secrets rotation.
- encrypt ```secret.yaml``` file from the previous example using private sealing key, and check it in to deploy secret config to clusters

1. Create private sealing key 

```

openssl req -x509 -nodes -newkey rsa:4096 -keyout "<path_to_sealing_key>/mytls1.key" -out "<path_to_sealing_key>/mytls1.crt" -subj "/CN=sealed-secret/O=sealed-secret"


```

<u><b>NOTE</b>: ```mytls1.key`` is <b>private key</b>, which is used by the controller to unseal secrets. It must be saved in a safe and not shared with anyone!</u>


2. Create secret manifest for sealing key, save as sealingkey.yaml
<u><b>NOTE:</b> this file must not be shared and will be disposed off in later steps.</u>

```yaml

apiVersion: v1
data:
  tls.crt: <base64 encoded content of mytls1.crt file>
  tls.key: <base64 encoded content of mytls1.key file>
kind: Secret
metadata:
  labels:
    sealedsecrets.bitnami.com/sealed-secrets-key: active
  name: sealed-secrets-custom-key
  namespace: kube-system
type: kubernetes.io/tls

```

3. Encrypt ``` sealingkey.yaml ``` and save it as ``` sealedsealingkey.yaml ```.
```

kubeseal <sealingkey.yaml >sealedsealingkey.yaml --controller-namespace kube-system --controller-name sealed-secrets --format yaml

```

4. Remove ``` sealingkey.yaml ```.  
Push ```sealedsealingkey.yaml``` to repo that's being monitored by flux.  
Wait a few minutes for flux sync to happen or run the following command to force the sync:

```
fluxctl sync --k8s-fwd-ns fluxcd

```

<u><b>Important:</b> delete sealed secrets controller pod so that the new sealing keys could be picked up.</u>

```
k delete pod -n kube-system sealed-secrets-<...>

```

5. Create secret manifest, save as secret.yaml (see previous sction for details)

6. Encrypt ``` secret.yaml ``` using private sealing key and save it as ``` sealedsecret.yaml ```.
```

kubeseal --cert "<path_to_sealing_key>/mytls1.crt" <secret.yaml >sealedsecret.yaml --controller-namespace kube-system --controller-name sealed-secrets --format yaml

```

7. Remove ``` secret.yaml ```.  
Push ```sealedsecret.yaml``` to repo that's being monitored by flux.
Wait a few minutes for flux sync to happen or run the following command to force the sync:

```
fluxctl sync --k8s-fwd-ns fluxcd

```

Verify that the new secret is there. 
 
```

kubectl get secret ngsa-secrets -o yaml

```

