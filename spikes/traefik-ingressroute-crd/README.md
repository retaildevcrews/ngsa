# Deploying NGSA Memory with Traefik middleware CRD

## Prerequisites

- A PnP Deployment with Traefik installed ([PnP deployment](https://github.com/retaildevcrews/caf-terraform-landingzones-starter/tree/main/enterprise_scale/construction_sets/aks/online/aks_secure_baseline)))

## Install NGSA-Memory app with Traefik IngressRoute and Middleware

- Change namespace and url-prefix arg to desired values in [ngsa-memory.yaml](./ngsa-memory.yaml) file.
  - Changing the namespace

    ```yaml
    apiVersion: v1
    kind: Namespace
    metadata:
      name: mock # Change: Namespace name
      labels:
        name: mock # Change: label
    ```

  - Changing the url-prefix arg

    ```yaml
    args: 
    - --in-memory
    - --prometheus
    - --url-prefix 
    - /mock # Change: the desired prefix
    ```

  - You can do it with the sed command below:

    ```bash
    nsname=somenamespace # Your app namespace
    urlpfx=/some-prefix  # Your app url prefix

    sed -i "s#namespace: .*#namespace: ${nsname}#g; s#name: mock#name: ${nsname}#g; s#/mock#${urlpfx}#g" ngsa-memory.yaml
    ```

### Deploy with IngressRoute

- Change the hostname, namespace and url-prefix in [ingressroute-middleware.yaml](./ingressroute-middleware.yaml) file.
  - You can do it with the `sed` command below:

  ```bash
  nsname=somenamespace # Your app namespace
  urlpfx=/some-prefix  # Your app url prefix
  hostname=somehost.domain.com

  # Change URL+Namesapce
  sed -i "s#namespace: .*#namespace: ${nsname}#g; s#/mock#${urlpfx}#g;" ingressroute-middleware.yaml

  # Change Hostname
   sed -i "s/Host.*\&\&/Host\(\`${hostname}\`\) \&\&/g" ingressroute-middleware.yaml
  ```

- Now apply the yaml files:

    ```bash
    # Apply the `ngsa-memory.yaml` first (it will create app's namespace):
    kubectl apply -f ./ngsa-memory.yaml
 
    # Link to Traefik CRDs: https://doc.traefik.io/traefik/reference/dynamic-configuration/kubernetes-crd/
    # Apply the Traefik CRDs
    kubectl apply -f ./traefik-crd.yaml

    # Apply the middleware and ingress route
    kubectl apply -f ./ingressroute-middleware.yaml
    ```

**IMPORTANT**, we need to add the App's namespace in the Traefik configuration, otherwise Traefik won't monitor that namespace for CRD resource changes.

This is because current Traefik deployment doesn't have a ClusterRoleBinding attached to it. Hence, it can only monitor the namespaces we provide access to.

Add the following lines under `[provider]` section:

```toml
[providers.kubernetesCRD]
  namespaces = ["our-app-namespace"]
```

Adding this configuration can be done manually (`kubectl edit`) or via a pre-configured configmap yaml file. The preconfigured yaml file is not provided because the config might be different and applying haphazardly it would delete the previous configmap.

> *Finding the configmap for Traefik:*
> 
> If the deployment was created following [Pnp CAF](https://github.com/retaildevcrews/caf-terraform-landingzones-starter/tree/main/enterprise_scale/construction_sets/aks/online/aks_secure_baseline) then there would be one Traefik configmap (traefik-ingress-config) under `a0008` namespace. Edit the configmap with `kubectl edit cm traefik-ingress-config`.
