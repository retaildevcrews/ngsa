# containerd Log Format Changes

> Unless I'm missing something, the move to containerd / cri-o is going to break everyone's logs as the CRI log format is totally different.

With the move to `containerd` as the runtime for Kubernetes, the log format changed. Below are additional samples of modifying fluent bit config to parse the new logs and are a supplement to this [repo](https://github.com/microsoft/fluentbit-containerd-cri-o-json-log).

This is what the logs look like with the docker shim. All of the fields are top level fields. This is what we need in order to not break our logging.

```json

[
    1610418623.542875065, 
    {
        "stream"=>"stdout", 
        "date"=>"2021-01-12T02:30:23.5428302Z", 
        "statusCode"=>200, 
        "path"=>"/log/app", 
        "duration"=>78, 
        "value"=>"CdFuTySNRb", 
        "kubernetes_pod_name"=>"logapp", 
        "kubernetes_namespace_name"=>"default", 
        "kubernetes_pod_id"=>"7e15c439-cd4a-48ba-8aa6-f67cd8d5590e", 
        "kubernetes_host"=>"k8s", 
        "kubernetes_container_name"=>"app", 
        "kubernetes_docker_id"=>"7e157d3c4426dfae3ab6eeb48e4342a2218d59252e9700479ff24fabc1967056", 
        "kubernetes_container_image"=>"docker.io/retaildevcrew/logapp:latest", 
        "kubernetes_labels_app"=>"logapp"
    }
]

```

This is what it looks like with the cri parser per the docs:

- Notice the new field logtag
- Notice that Kubernetes is a map
- Notice that the app log values (date, statusCode, etc) are inside message
  - But, message is not a map, it's a json string

[1-fluentbit-sample.yaml](./1-fluentbit-sample.yaml)

```json

[
    1610418076.918769493, 
    {
        "stream"=>"stdout", 
        "logtag"=>"F", 

        "message"=>"
        {
            "date":"2021-01-12T02:21:16.9186922Z",
            "statusCode":200,"path":"/log/app",
            "duration":88,
            "value":"KEqGyQKSYG"
        }", 

        "kubernetes"=>
        {
            "pod_name"=>"logapp", 
            "namespace_name"=>"default", 
            "pod_id"=>"d0ea6ebf-ab5c-4a1f-8d23-d24edd840614", 
            "labels"=>{"app"=>"logapp"}, "host"=>"k8s", 
            "container_name"=>"app", 
            "docker_id"=>"ee66930c7909b5b651ec347c2740397757157e3801383064566ab6943c9d757c", 
            "container_image"=>"docker.io/retaildevcrew/logapp:latest"
        }
    }
]

```

If you change 'message' in cri filter to 'log', this is what it looks like:

[2-log-map.yaml](./2-log-map.yaml)

```json

[
    1610418355.606844040, 
    {
        "stream"=>"stderr", 
        "logtag"=>"F", 
        "date"=>"2021-01-12T02:25:55.6063703Z", 
        "statusCode"=>500, 
        "path"=>"/log/app", 
        "duration"=>720, 
        "message"=>"Server error 9603", 

        "kubernetes"=>
        {
            "pod_name"=>"logapp", 
            "namespace_name"=>"default", 
            "pod_id"=>"e94fb359-7877-4f5e-a0de-948b4fdf04c0", 
            "labels"=>{"app"=>"logapp"}, "host"=>"k8s", 
            "container_name"=>"app", 
            "docker_id"=>"fd363f1085fa9b5edfae8f5b94159bd26babfacf6276c29c0ec84a146afa140d", 
            "container_image"=>"docker.io/retaildevcrew/logapp:latest"
        }
    }
]

```

If you add a lift on Kubernetes and a lift on labels, you get the same as before with the addition of logtag

- This is the only option that won't 'break' your current logging
- To be the 'same', you would need to remove logtag (we didn't)

[3-working-log.yaml](./3-working-log.yaml)

```json

[
    1610418623.542875065, 
    {
        "stream"=>"stdout", 
        "logtag"=>"F", 
        "date"=>"2021-01-12T02:30:23.5428302Z", 
        "statusCode"=>200, 
        "path"=>"/log/app", 
        "duration"=>78, 
        "value"=>"CdFuTySNRb", 
        "kubernetes_pod_name"=>"logapp", 
        "kubernetes_namespace_name"=>"default", 
        "kubernetes_pod_id"=>"7e15c439-cd4a-48ba-8aa6-f67cd8d5590e", 
        "kubernetes_host"=>"k8s", 
        "kubernetes_container_name"=>"app", 
        "kubernetes_docker_id"=>"7e157d3c4426dfae3ab6eeb48e4342a2218d59252e9700479ff24fabc1967056", 
        "kubernetes_container_image"=>"docker.io/retaildevcrew/logapp:latest", 
        "kubernetes_labels_app"=>"logapp"
    }
]

```

Instead of renaming 'message' to 'log' in the cri filter, you can call a docker parser filter on message (make sure Reserve_Data is On). Changing to log is the easiest but it works either way and we have logs flowing!

[4-working-message.yaml](./4-working-message.yaml)
