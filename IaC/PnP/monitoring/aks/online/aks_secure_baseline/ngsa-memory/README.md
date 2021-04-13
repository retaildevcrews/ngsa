# Deploying NGSA Memory Manually

## Prerequisites

- A PnP AKS Deployment with Traefik

## Install NGSA-Memory

- Apply the `ngsa-memory.yaml`

    ```bash
    kubectl apply -f ./ngsa-memory
    ```
- Apply the new traefik configuration

    ```bash
    # REPO_ROOT=Root directory of this repository
    kubectl apply -f ${REPO_ROOT}/enterprise_scale/construction_sets/aks/online/aks_secure_baseline/workloads/baseline/traefik.yaml
    ```

## Notes

- Traefik yaml changes at [./../workloads/baseline/traefik.yaml](./../workloads/baseline/traefik.yaml)
  - Added a new RoleBinding for the ServiceAccount `traefik-ingress-controller` in `ngsa` namespace
  - In `traefik-ingress-config` ConfigMap
    - Added `ngsa` namespace in `traefik.toml` under `[providers.kubernetesingress]`
    - Added a new middleware to strip the `/memory` and `/cosmos` path prefix from the URL under `[http.middlewares.ngsa-stripprefix.stripPrefix]`


## Deploying another app

To deploy another application (say App-A) in another namespace (app-ns), these steps needs to be taken:

- Traefik Configmap: `traefik-ingress-config` in [traefik.yaml](./../workloads/baseline/traefik.yaml) 
  - Modify the `traefik-ingress-config` ConfigMap in [traefik.yaml](./../workloads/baseline/traefik.yaml) file and add the `app-ns` in `namespaces` under `[providers.kubernetesingress]`

    ```toml
    [providers.kubernetesingress]
    ingressClass = "traefik-internal"
    namespaces = ["a0008", "ngsa", "app-ns"]
    ```

  - Add new middleware if the app needs to be path prefixed
    - The whole url will get passed to the app, so if the app can handle that no need to strip the path
    - Otherwise, path needs to be stripped
    - Add the app's path to the ConfigMap under `[http.middlewares.ngsa-stripprefix.stripPrefix]`

        ```toml
        [http.middlewares.ngsa-stripprefix.stripPrefix]
            prefixes = ["/memory", "/cosmos","/app-a-prefix"]
        ```

- In your App's Ingress object
  - Add traefik's annotation and
  - Specify the middleware as `ngsa-stripprefix@file`
  - Under rules specify the same path prefix which was specified in traefik config
  - The final Yaml for app-a would look like below:

    ```yaml
    apiVersion: networking.k8s.io/v1
    kind: Ingress
    metadata:
    name: app-a-ingress
    namespace: app-ns
    annotations:
        kubernetes.io/ingress.allow-http: "false"
        kubernetes.io/ingress.class: traefik-internal
        traefik.ingress.kubernetes.io/router.entrypoints: websecure
        traefik.ingress.kubernetes.io/router.tls: "true"
        traefik.ingress.kubernetes.io/router.tls.options: default
        traefik.ingress.kubernetes.io/router.middlewares: ngsa-stripprefix@file
        # The annotation above makes sure middleware ngsa-stripprefix is applied to URL
    spec:
    tls:
    - hosts:
        - app-a.sample-host.com
    rules:
    - host: app-a.sample-host.com
        http:
        paths:
        - path: /app-a-prefix # Same as in traefik configmap
            pathType: Prefix
            backend:
            service:
                name: app-a-svc
                port:
                number: 8080
    ```

- We need to attach the `app-ns` namespace to the `traefik-ingress-controller` ServiceAccount. To do that we need to apply a new RoleBinding

    ```yaml
    cat <<EOF | kubectl apply -f -
        kind: RoleBinding
        apiVersion: rbac.authorization.k8s.io/v1
        metadata:
            name: traefik-watch-workloads
            namespace: app-ns # Namespace where the app is
            labels:
            app.kubernetes.io/name: traefik-ingress-ilb
            app.kubernetes.io/instance: traefik-ingress-ilb
        roleRef:
            apiGroup: rbac.authorization.k8s.io
            kind: ClusterRole
            name: traefik-ingress-controller
        subjects:
        - kind: ServiceAccount
            name: traefik-ingress-controller
            namespace: a0008 # This is the svc account ns, where traefik is
    EOF
    ```

- Apply your app's yaml file
