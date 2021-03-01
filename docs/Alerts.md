
# Alerts

The following describes the proposed alerts for our ngsa pre-prod environment based off of log analytics queries from `ngsa-pre-log`.  The thresholds and triggers will likely need to be adjusted as we understand in practice how often they are actually triggered and how useful they are (i.e. are they false positives, too sensitive, etc.).  

## Reliability

### Failed Server Requests

- Per zone and pod type (cosmos/memory)
- **Purpose**: Notify of failures on server side
- **Metric**: Count of status code >=400
- **Threshold**: >20 failed requests over last 5 minutes
- **Trigger**: If threshold is breached >=1 time in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **2 Queries/Alerts**: One for each pod type, will trigger per zone

``` bash

  # Failed Server Requests (ngsa-cosmos)

  ngsa_CL
  | where k_container_s == 'app' and k_app_s == 'ngsa-cosmos' and StatusCode_d >= 400
  | summarize AggregatedValue=count() by bin(TimeGenerated, 5m), Zone_s
  
  # Failed Server Requests (ngsa-memory)

  ngsa_CL
  | where k_container_s == 'app' and k_app_s == 'ngsa-memory' and StatusCode_d >= 400
  | summarize AggregatedValue=count() by bin(TimeGenerated, 5m), Zone_s

```

### Failed Client Requests (loderunner)

- Per zone and app pod type (cosmos/memory)
- **Purpose**: Notify of failures on client side
- **Metric**: Count of status code >=400
- **Threshold**: >20 failed requests over last 5 minutes
- **Trigger**: If threshold is breached >=1 time in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **2 Queries/Alerts**: One for each pod type, will trigger per zone

```bash

  # Failed Client Requests (ngsa-cosmos)

  loderunner_CL
  | where Server_s contains 'ngsa-cosmos' and StatusCode_d >= 400
  | summarize AggregatedValue=count() by bin(TimeGenerated, 5m), Zone_s

  # Failed Client Requests (ngsa-memory)

  loderunner_CL
  | where Server_s contains 'ngsa-memory' and StatusCode_d >= 400
  | summarize AggregatedValue=count() by bin(TimeGenerated, 5m), Zone_s

```

### Too Few Server Requests

- Per zone and pod type (cosmos/memory)
- **Purpose**: Notify if loderunner is down (not sending requests) or ngsa app is down
- **Metric**: Server requests per minute
- **Threshold**: < 5 requests over last minute
- **Trigger**: If threshold is breached >4 times in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **2 Queries/Alerts**: One for each pod type, will trigger per zone

```bash

  # Too Few Server Requests (ngsa-cosmos)

  ngsa_CL
  | where k_container_s == 'app' and k_app_s == 'ngsa-cosmos'
  | summarize AggregatedValue=count() by bin(TimeGenerated,1m), Zone_s

  # Too Few Server Requests (ngsa-memory)

  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == 'ngsa-memory'
  | summarize AggregatedValue=count() by bin(TimeGenerated,1m), Zone_s

```

### Too Few Client Requests (loderunner)

- Per zone and app pod type (cosmos/memory)
- **Purpose**: Notify if loderunner is down (not sending requests) or fluent bit is not reporting logs
- **Metric**: Client requests per minute
- **Threshold**: <5 requests over last minute
- **Trigger**: If threshold is breached >4 times in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **2 Queries/Alerts**: One for reach pod type, will trigger per zone

```bash

  # Too Few Client Requests (ngsa-cosmos)

  loderunner_CL
  | where Server_s contains 'ngsa-cosmos'
  | summarize AggregatedValue=count() by bin(TimeGenerated,1m), Zone_s

  # Too Few Client Requests (ngsa-memory)

  loderunner_CL
  | where Server_s contains 'ngsa-memory'
  | summarize AggregatedValue=count() by bin(TimeGenerated,1m), Zone_s

```

## Performance

### High Server Response Time

- Per zone and pod type (cosmos/memory)
- **Purpose**: Notifies of unexpected perf hit and increased outliers
- **Metric**: 95th %tile response time per minute
- **Thresholds**:
  - ngsa-cosmos:
    - Az-EastUS2: >130ms
    - Az-CentralUS: >26ms
    - Az-WestUS2: >100ms
  - ngsa-memory: >9ms
- **Trigger**: If threshold is breached >4 times in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **4 Queries/Alerts**:

```bash

  # High Server Response Time in CentralUS (ngsa-cosmos)

  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == 'ngsa-cosmos' and Zone_s == "Az-CentralUS"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Server Response Time in EastUS2 (ngsa-cosmos)

  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == 'ngsa-cosmos' and Zone_s == "Az-EastUS2"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Server Response Time in WestUS2 (ngsa-cosmos)

  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == 'ngsa-cosmos' and Zone_s == "Az-WestUS2"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Server Response Time (ngsa-memory)

  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == 'ngsa-memory'
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m), Zone_s

```

### High Client Response Time (loderunner)

- Per zone and app pod type
- **Purpose**: Notifies of unexpected perf hit and increased outliers
- **Metric**: 95th %tile response time per minute
- **Thresholds**:
  - ngsa-cosmos:
    - Az-EastUS2: >130ms
    - Az-CentralUS: >30ms
    - Az-WestUS2: >105ms
  - ngsa-memory: >11ms
- **Trigger**: If threshold is breached >4 times in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **4 Queries/Alerts**:

```bash

  # High Client Response Time in CentralUS (ngsa-cosmos)

  loderunner_CL
  | where Server_s contains 'ngsa-cosmos' and Zone_s == "Az-CentralUS"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Client Response Time in EastUS2 (ngsa-cosmos)

  loderunner_CL
  | where Server_s contains 'ngsa-cosmos' and Zone_s == "Az-EastUS2"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Client Response Time in WestUS2 (ngsa-cosmos)

  loderunner_CL
  | where Server_s contains 'ngsa-cosmos' and Zone_s == "Az-WestUS2"
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m)

  # High Client Response Time (ngsa-memory)

  loderunner_CL
  | where Server_s contains == 'ngsa-memory'
  | summarize AggregatedValue=percentile(Duration_d, 95) by bin(TimeGenerated, 1m), Zone_s

```

## Expected Usage

### Too Many Server Requests

- Per zone and pod type (cosmos/memory)
- **Purpose**: Notifies of unexpected spike in traffic
- **Metric**: Server requests per minute
- **Threshold**: >225
- **Trigger**: If threshold is breached >4 times in last 10 minutes
- **Period**: 10 minutes
- **Frequency**: 10 minutes
- **Suppress Alerts**: Wait 30 minutes before alerting again
- **2 Queries/Alerts**:

```bash

  # Too Many Server Requests (ngsa-cosmos)
  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == "ngsa-cosmos"
  | summarize AggregatedValue=count() by bin(TimeGenerated, 1m), Zone_s

  # Too Many Server Requests (ngsa-memory)
  ngsa_CL 
  | where k_container_s == 'app' and k_app_s == "ngsa-memory"
  | summarize AggregatedValue=count() by bin(TimeGenerated, 1m), Zone_s

```
