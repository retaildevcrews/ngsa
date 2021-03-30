- I think we should take all of the below items and have a few people do a spike on scale testing
- followed by a design review and necessary code / infra changes
- i see (at least) two patterns
  - your backend services don't autoscale
    - your backend services do autoscale
    - i think the code and how you respond is different in each scenario
    - i think our existing approach is tuned for the 2nd scenario - would like to test to verify

We ran series of tests to setup a consistent baseline for Cosmos throttling. Tests were ran with the following params:

- Single AKS cluster with 3 nodes
- KEDA disabled
- One NGSA App and no loderunner(l8r)
- All logs (from apps and l8r) were sent to Log Analytics
- Nodes configuration: DS2_V2, (for more mem/cpu D4s_v3)

Measurements:

- QPS (Queries Per Second) from app and l8r
- Throttling from Cosmos (429s)
- 429s from app/l8r

## Cosmos Throttle Test on a dev cluster

- _QPS = Queries per second_

| **CosmosScale**  | **MoreCPUMem** | **Cosmos Throttle** | **App 429s**
|------------------|------------|------------------|---------------
| No (400 RU)      | No         | Throttled at 100 QPS | 429s at 150 QPS
| No (1000 RU)     | No         | Throttled at 300 QPS | 429s at 400 QPS
| Yes (400-4000 RU)**| No       | At 1000 QPS, didn't throttle, </br>Cosmos Scaled to 1100 RU</br> App CPU Usage 780m| N/A

> Results were same for a cluster with more CPU/MEM

> ** Note on Cosmos autoscale: Autoscale test didn't behave as expected. It should've increased to at least 3000RU for 1000 QPS. Possible reasons: (i) app/l8r wasn't pushing as much as it should have or (ii) the testing scenario was faulty

### Kusto Queries

Below are the queries we used to check all the relevant metrics:

#### _Get App Metrics_

```kusto

ngsa_CL
| where  k_container_s == "app"  and k_app_s == "ngsa-aks" 
    and LogName_s == "Ngsa.RequestLog"
    | summarize ThrottledRequests=countif(StatusCode_d == 429) , SucceedRequests=countif(StatusCode_d == 200) , TotalCount=count()
```

#### _Get Cosmos Metrics_

```kusto

AzureDiagnostics
| where ResourceProvider=="MICROSOFT.DOCUMENTDB" and Category == "DataPlaneRequests" and Resource == "NGSA-TRIPLET-TEST-COSMOS-DB"
| where databaseName_s == "imdb"
| summarize ThrottledRequests=countif(toint(statusCode_s) == 429) , SucceedRequests=countif(toint(statusCode_s) == 200) , TotalCount=count()
```

#### _Get Loderunner Metrics_

```kusto
loderunner_CL 
| where kubernetes_labels_app_s == "l8r-LABEL"
| summarize  count()
```

> We used separate labels for each loderunner "l8r-LABEL"

## Test in PreProd

LoadTest with webv:

cmd:

```bash
webv -s https://ncpc.cse.ms \
        https://ncpe.cse.ms \
        https://ncpw.cse.ms \
        https://nmpc.cse.ms \
        https://nmpe.cse.ms \
        https://nmpw.cse.ms \
        -f baseline.json benchmark.json \
        -r --max-errors 1 \
        -l MILLI_SEC --duration 120
```

> We chose different `MILLI_SEC` for each test
### Throttling Result

- 150 QPS (`-l 20` on each cluster) didn't throttle
- 200 QPS (`-l 15` on each cluster) didn't throttle
- 250 QPS (`-l 12` on each cluster) throttled: 19 count 429s
- 300 QPS (`-l 10` on each cluster) throttled: 264 count 429s

So >= 250 QPS on one a single CosmosDB (1000 RU without Autoscalling) will throttle.
