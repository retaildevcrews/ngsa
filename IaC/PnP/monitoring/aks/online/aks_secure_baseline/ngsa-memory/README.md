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
    kubectl apply -f ./workloads/baseline/traefik.yaml
    ```
- Create a Flux CD instance for the NGSA app

    ```bash
    kubectl apply -f ./ngsa-settings/ngsa_memory_flux.yaml 
    ```

## Notes

- Traefik yaml changes at [./../workloads/baseline/traefik.yaml](./../workloads/baseline/traefik.yaml)
  - Added a new RoleBinding for the ServiceAccount `traefik-ingress-controller` in `ngsa` namespace
  - In `traefik-ingress-config` ConfigMap
    - Added `ngsa` namespace in `traefik.toml` under `[providers.kubernetesingress]`
    - Added a new middleware to strip the `/memory` and `/cosmos` path prefix from the URL under `[http.middlewares.ngsa-stripprefix.stripPrefix]`
- Before creating the Flux CD instance, the settings in the yaml file [./../ngsa-settings/ngsa_memory_flux.yaml](./../ngsa-settings/ngsa_memory_flux.yaml) need to be verified
  - The gitops repo is specified in the yaml file using the variable `--git-url`
  - The path in the gitops repo is specified in the yaml file using the variable `--git-path`. There should be a yaml file for ngsa-memory in the specified folder.
  - There should be a `config.json` file one folder above the path specified in the yaml file by `--git-path`. Ensure that the variables set in this json file (hostname, url prefix, agentpool, etc) are correct, as they are needed by autogitops to enable the automated generation of the `ngsa-memory.yaml` file.


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

    ```bash
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

- (Optional) Add a Flux CD instance for the app (see [this sample yaml file](../ngsa-settings/ngsa_memory_flux.yaml) for an example). Flux CD instances automatically ensure that the state of resources deployed in the AKS cluster match their description in a GIT repository. The Flux CD instance routinely compares the deployed resources to the resource descriptions at the specified path in the specified repository and updates the deployed resources automatically to match the repository if there is any difference. To deploy a Flux CD instance for the newly deployed app:
  - Update the following fields in the [yaml file](../ngsa-settings/ngsa_memory_flux.yaml):
    - --git-url=""          
    The ```git-url``` field should point to the repository you wish to watch for updates
    - --git-branch=""       
    The ```git-branch``` field should be set to "main", unless you have a reason to watch a specific branch of the repository.
    - --git-path=""         
    The ```git-path``` fields is the path to the folder in the repository containing the yaml files that describe the desired state of the app's resources in our AKS cluster (```ngsa-memory.yaml``` in this case)
    - --k8s-secret-name=""  
    The ```k8s-secret-name``` field sets the name of the secret used by your Flux CD instance. The secret is defined in the yaml file, so its name should be updated too. The default value is ```flux-git-deploy```, but this is being updated to prevent confusion because more than one Flux CD instances is in use in the AKS cluster. 
    - All resource names and namespaces
  - Apply the app's flux yaml file: 
  
    ```bash 
    kubectl apply -f ./ngsa-settings/ngsa_memory_flux.yaml
    ```
  - Check flux instance using the command ***kubectl logs [flux pod name] -n [flux pod namespace]*** e.g.,
    ```bash
    kubectl logs ngsa-flux-698f4b466c-rvc7q -n ngsa-memory-settings
    ```