# Log Data Dictionary

## App / DS Log

```json

{
    <!-- CRI Log -->
    "stream": "stdout",
    "logtag": "F",

    <!-- App Log -->
    "Date": "2021-01-21T21:05:55.9327217Z",
    "LogName": "Ngsa.RequestLog",
    "StatusCode": 404,
    "TTFB": 0.220000,
    "Duration": 0.220000,
    "Verb": "GET",
    "Path": "/api/movies/tt0133093/foo",
    "Host": "ngsa-memory:4120",
    "ClientIP": "10.244.0.84",
    "UserAgent": "l8r/0.0.8",
    "CVector": "Qe80SCa3xky9QzZGd0Ai+Q.0.0",
    "CVectorBase": "Qe80SCa3xky9QzZGd0Ai+Q",

    <!-- App Log -->
    "DataService": "localhost:4122",

    <!-- Data Service Log -->
    "CosmosName": "ngsa-pre-westus2",
    
    <!-- inserted from config -->
    "Region": "Dev",
    "Zone": "Dev",

    <!-- mapped from kubernetes_* -->
    "k_app": "ngsa-memory",
    "k_container": "app",
    
    <!-- maps to k_* -->
    "kubernetes_pod_name": "ngsa-memory",
    "kubernetes_container_name": "app",

    <!-- from kubernetes fluent bit filter -->
    "kubernetes_namespace_name": "default",
    "kubernetes_pod_id": "2e39e645-4d73-46e4-9dbc-9bc7e9f406d6",
    "kubernetes_host": "k8s",
    "kubernetes_docker_id": "4bf9d82f5a73abb10973033d4d5c5669d97910474a419f08f74abc28185a5423",
    "kubernetes_container_hash": "docker.io/retaildevcrew/4bf9...5423",
    "kubernetes_container_image": "docker.io/retaildevcrew/ngsa-app:beta",
    "kubernetes_labels_app": "ngsa-memory"
}

```

## LodeRunner Log

```json

{
    <!-- CRI Log -->
    "stream": "stdout",
    "logtag": "F",

    <!-- LodeRunner Log -->
    "Date": "2021-01-21T22:20:18.4161098Z",
    "Server": "http://ngsa-memory:4120",
    "StatusCode": 404,
    "Verb": "GET",
    "Path": "/api/movies/tt0133093/foo",
    "Errors": 0,
    "Duration": 0.780000,
    "ContentLength": 0,
    "CVector": "DTDq9dD9GU2eu8cl047WHw.0",
    "CVectorBase": "DTDq9dD9GU2eu8cl047WHw",
    "Tag": nil,
    "Quartile": 1,
    "Category": "DirectRead",

    <!-- inserted from config -->
    "Region": "Dev",
    "Zone": "Dev",

    <!-- mapped from kubernetes_* -->
    "k_app": "loderunner-baseline-memory",
    "k_container": "app",
    
    <!-- maps to k_* -->
    "kubernetes_pod_name": "loderunner-baseline-memory",
    "kubernetes_container_name": "app",

    <!-- from kubernetes fluent bit filter -->
    "kubernetes_namespace_name": "default",
    "kubernetes_pod_id": "312a3ce8-bcbe-4ea7-9842-17bf5a302f21",
    "kubernetes_host": "k8s",
    "k_container": "app",
    "kubernetes_docker_id": "60a0edb946ab2dfbd09b6c37cc7fba9b745c1c819cb178171edb6da9900438ce",
    "kubernetes_container_hash": "docker.io/retaildevcrew/ngsa-loderunner@sha256:9c88...6be3",
    "kubernetes_container_image": "docker.io/retaildevcrew/ngsa-loderunner:beta",
    "kubernetes_labels_app": "loderunner-baseline-memory",
}

```
