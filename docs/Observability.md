# Observability

## Dashboard Setup

Replace the values in the `NGSADashboard.json` file surrounded by %% with the proper environment variables which are set in the [IaC/AKS/README.md](../IaC/AKS/README.md) walkthrough. The required variables from the walkthrough are: Ngsa_Log_Analytics_RG, Ngsa_Log_Analytics_Name, Imdb_RG, and Imdb_Name.

> Note, currently the implementation assumes that the custom log table in Log Anayltics is ngsa_CL.

```bash

# make sure you are logged in with the correct Azure subscription
az account show

# set the subscription id environment variable
export Ngsa_Sub='az account show -o tsv --query id'

# copy the dashboard template file to a local version that will be updated with deployment-specific values
cp NGSA_Dashboard_Template.json NGSA_Dashboard.json

# replace the variables in the template with the appropriate environment variables
sed -i "s/%%SUBSCRIPTION_GUID%%/$(eval $Ngsa_Sub)/g" NGSA_Dashboard.json
sed -i "s/%%Ngsa_Log_Analytics_RG%%/${Ngsa_Log_Analytics_RG}/g" NGSA_Dashboard.json
sed -i "s/%%Imdb_RG%%/${Imdb_RG}/g" NGSA_Dashboard.json
sed -i "s/%%Imdb_Name\%%/${Imdb_Name}/g" NGSA_Dashboard.json
sed -i "s/%Ngsa_Log_Analytics_Name%%/$Ngsa_Log_Analytics_Name}/g" NGSA_Dashboard.json

```

Navigate to the ([dashboard](https://portal.azure.com/#dashboard)) within your Azure portal. Click upload and select the `NGSA_Dashboard.json` file that you have created.

## Metrics

There are two kinds of metrics displayed in this dashboard: default Azure Monitor metrics for Cosmos DB and query-based metrics from Log Analytics.

### Azure Monitor - Cosmos DB

The top section of the dashboard shows three metrics using built-in Cosmos DB usage data from Azure Monitor.

* The total number of requests hitting the Cosmos DB instance and the Total Request Units (RUs) the resulting queries cost.
* The total number of throttled requests (if any). This is acheived by filtering to requests with a status code of 429.
* The provisioned throughput to help monitor that the configuration has not been unexpectedly changed.

### Log Analytics - Query-based

The remaining sections of the dashboard focuses on the performance and reliability of the apps (ngsa-cosmos, ngsa-memory, and webv) running on the clusters. The following queries are used to generate the metrics.

> ngsa-cosmos is the app querying against Cosmos DB and ngsa-memory is the app querying against in-memory data.
> PodType 'webv' is filtered out in some queries as they are focused on the ngsa apps and webv also logs to the same table.

#### Successful Server Requests

```bash

# summary table of server request success rate for each ngsa app
ngsa_CL
| where PodType_s != 'webv'
| summarize RequestSuccessRate = 100.0*countif(StatusCode_d < 400)/count() by PodType_s
| project-rename PodType=PodType_s

# success rate of server requests handled by ngsa-cosmos, aggregated over every 10m
ngsa_CL
| where PodType_s == 'ngsa-cosmos'
| summarize RequestSuccessRate = 100.0*countif(StatusCode_d < 400)/count() by bin(Date_t,10m)
| project-rename Date=Date_t

# success rate of server requests handled by ngsa-memory, aggregation: 10m
ngsa_CL
| where PodType_s == 'ngsa-memory'
| summarize RequestSuccessRate = 100.0*countif(StatusCode_d < 400)/count() by bin(Date_t,10m)
| project-rename Date=Date_t

```

#### Failed Server Requests

```bash

# summary table of the number of failed server requests by status code for each app
ngsa_CL
| where PodType_s != 'webv'
| where StatusCode_d > 200
| summarize count() by PodType_s, tostring(toint(StatusCode_d))
| project-rename PodType=PodType_s,RequestCount=count_,StatusCode=StatusCode_d

# number of failed requests handled by ngsa-cosmos, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-cosmos'
| where StatusCode_d > 200
| summarize count() by bin(Date_t,1m), tostring(toint(StatusCode_d))
| project-rename Date=Date_t,FailedRequests=count_,StatusCode=StatusCode_d

# number of failed requests handled by ngsa-memory, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-memory'
| where StatusCode_d > 200
| summarize count() by bin(Date_t,1m), tostring(toint(StatusCode_d))
| project-rename Date=Date_t,FailedRequests=count_,StatusCode=StatusCode_d

```

#### Server Response Time

```bash

# summary table of average server response time (ms) for each app
ngsa_CL
| where PodType_s != 'webv'
| summarize avg(Duration_d) by PodType_s
| project-rename PodType=PodType_s,ResponseTime=avg_Duration_d

# average response time of the ngsa-cosmos app, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-cosmos'
| summarize avg(Duration_d) by bin(Date_t,1m)
| project-rename Date=Date_t,ResponseTime=avg_Duration_d

# average response time of the ngsa-memory app, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-memory'
| summarize avg(Duration_d) by bin(Date_t,1m)
| project-rename Date=Date_t,ResponseTime=avg_Duration_d

```

#### Server Requests

```bash

# summary table of average server requests per minute, per app, per zone
ngsa_CL
| where PodType_s != 'webv'
| summarize count() by bin(Date_t,1m), PodType_s, Zone_s
| summarize avg(count_) by PodType_s, Zone_s 
| project-rename AvgRequests=avg_count_, PodType=PodType_s, Zone=Zone_s
| order by PodType asc

# number of server requests by zone for the ngsa-cosmos app, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-cosmos'
| summarize count() by bin(Date_t,1m), Zone_s
| project-rename Date=Date_t,ServerRequests=count_, Zone=Zone_s

# number of server requests by zone for the ngsa-memory app, aggregation: 1m
ngsa_CL
| where PodType_s == 'ngsa-memory'
| summarize count() by bin(Date_t,1m), Zone_s
| project-rename Date=Date_t,ServerRequests=count_, Zone=Zone_s


```

#### Failed Server Requests (detailed)

```bash

# table of failed requests with status code and request path for the ngsa-cosmos app
ngsa_CL
| where PodType_s == 'ngsa-cosmos'
| where StatusCode_d > 200
| summarize count() by tostring(toint(StatusCode_d)), Path_s
| project-rename StatusCode=StatusCode_d,RequestCount=count_,Path=Path_s

# table of failed requests with status code and request path for the ngsa-memory app
ngsa_CL
| where PodType_s == 'ngsa-memory'
| where StatusCode_d > 200
| summarize count() by tostring(toint(StatusCode_d)), Path_s
| project-rename StatusCode=StatusCode_d,RequestCount=count_,Path=Path_s

```

#### WebV Average Response Times

```bash

# table of average response times as observed by the client, by server and zone
ngsa_CL
| where PodType_s == 'webv'
| summarize avg(Duration_d) by Server_s, Zone_s
| project-rename ResponseTime=avg_Duration_d, Server=Server_s, Zone=Zone_s
| order by Server, Zone

```

#### WebV Failures

```bash

# table of failures observed by the client, by server and zone
ngsa_CL
| where PodType_s == 'webv'
| summarize countif(StatusCode_d > 200) by Server_s, Zone_s
| project-rename FailureCount=countif_, Server=Server_s, Zone=Zone_s
| order by Server, Zone

```
