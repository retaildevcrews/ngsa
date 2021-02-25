# Prometheus and Log analytics integration using OMS Agent

Prometheus is a popular open source metric monitoring solution and is a part of the Cloud Native Compute Foundation. Azure Monitor for containers provides a seamless onboarding experience to collect Prometheus metrics. Typically, to use Prometheus, you need to set up and manage a Prometheus server with a store. By integrating with Azure Monitor, a Prometheus server is not required. You just need to expose the Prometheus metrics endpoint through your exporters or pods (application), and the containerized agent for Azure Monitor for containers can scrape the metrics.

Reference Documentation [here](https://docs.microsoft.com/en-us/azure/azure-monitor/containers/container-insights-prometheus-integration).

## Steps to Scrape Metrics into Log Analytics

- Save the Workspace ID and the key for log Analytics work space from the Azure dashboard
- To use OMS Agent Daemonset and deployment: (Skip this step if OMS is already running in the AKS by using command kubectl get pods -A and check if omsagent is running in kube-system namespace)
  - Create the log analytics secrets with kubectl:

    ```bash

    kubectl create secret generic omsagent-secret -n kube-system \
                --from-literal=KEY=$(az monitor log-analytics workspace get-shared-keys \
                -g ngsa-fb-test-test-rg -n ngsa-fb-test-log --query primarySharedKey -o tsv) \
                --from-literal=WSID=$(az monitor log-analytics workspace show \
                -g ngsa-fb-test-test-rg -n ngsa-fb-test-log --query customerId -o tsv)
    # If you don't have az cli access, replace the $(az monitor...) portion with actual values
    
    ```

    - Check secrets created, run `kubectl get secrets omsagent-secret -n kube-system -o jsonpath='{.data}'`
  - Properly annotate target pods so that Prometheus can select and scrape the pod metrics. For details on proper annotations for OMS agent see [this link](https://docs.microsoft.com/en-us/azure/azure-monitor/containers/container-insights-prometheus-integration)

    ```bash
    
    # This will annotate the pod instance "ngsa-memory"
    kubectl annotate pod ngsa-memory prometheus.io/scrape=true --overwrite prometheus.io/path='/metrics' prometheus.io/port=8080 prometheus.io/scheme=http interval=30s --overwrite

    # Check ngsa-memory has the correct annotations
    kubectl get pod ngsa-memory -o jsonpath='{.metadata.annotations}' | jq

    ```

  - Apply the OMS yaml files: `kubectl apply -f .`
  - Check if resources are created properly:
    - Configmap: `kubectl get cm container-azm-ms-agentconfig -n kube-system`
    - Secrets:  `kubectl get secrets omsagent-secret -n kube-system`
    - Daemonset: `kubectl get ds omsagent -n kube-system`
  - Check the OMS agent logs with command and it should indicate the Prometheus config processing
    ``` kubectl logs -f <omsagent-pod-name> --namespace kube-system ```

      ![Image](agent-logs.png)

- Now go into the logs tab of the Log analytics workspace and type the query below you should be able to see the prometheus metrics in the azure log analytics

  ` InsightsMetrics | where Namespace contains "prometheus" | sort by TimeGenerated desc  `

  ![Image](workspace-query-results.png)

> More about dashboards with queries . Reference : https://docs.microsoft.com/en-us/azure/azure-monitor/visualize/tutorial-logs-dashboards

