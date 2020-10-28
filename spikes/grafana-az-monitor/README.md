# Adding Azure Monitor source in Grafana

Following instructions allow adding Azure Monitor plugin as a source and create custom Grafana dashboards in an existing Istio setup

## Prerequisites
- An AKS-Istio cluster following the [IaC-AKS readme][1]
- Permission to add a service principal to a resource group in Azure Portal/CLI

## Setup

Knowing the resource group prefix `$Ngsa_Name` set in the [IaC-AKS readme][1] will help follow this guide.

Name definitions:
- Common Name Prefix: `$Ngsa_Name`
- Service Principal: `$Ngsa_AKS_Name`
  - `Ngsa_AKS_Name = ${Ngsa_Name}-aks`  Typically it is the same as the Kubernetes cluster name.
- Cosmos resource group: `$Imdb_RG`
  - `Imdb_RG = ${Ngsa_Name}-rg-cosmos`
- App Resource group: `$Ngsa_App_RG`
  - `Ngsa_App_RG = ${Ngsa_Name}-rg-app`
- App Insights: `$Ngsa_Name`

If IaC readme was followed, there will be two resource groups created.

For example, if `Ngsa_Name=foobar`, then two resource group names will be: `foobar-rg-app` and `foobar-rg-cosmos`.

### Install Azure Monitor Plugin for Grafana
Execute into the Grafana pod.
```bash
# Assuming deafult istio namespace "istio-system"
kubectl exec -it -n istio-system $(k get pods -n istio-system -l app=grafana -o jsonpath='{.items[0].metadata.name}') -- bash
```

If the above command errors out, then try the following
``` bash
# Get Grafana's pod name, assuming default istio namespace "istio-system"
kubectl get pods -n istio-system -l app=grafana
# Take a note of grafana's pod name. e.g: grafana-acf9714c74-h6725
# Now execute into the pod
kubectl exec -it -n istio-system <<GRAFANA-POD-NAME>> -- bash
```

Now install Azure Monitor plugin inside the pod using `grafana-cli`
```bash
grafana-cli plugins install grafana-azure-monitor-datasource
```

### Adding required permissions/secrets from Azure Portal
A new service principal can be created for Grafana access but this guide will reuse the `$Ngsa_AKS_Name` service principal.

#### Add SP to cosmos resoruce group
Add `$Ngsa_AKS_Name` SP to the cosmos resource group `$Imdb_RG`:
- Goto resource group `$Imdb_RG` --> Access Control (IAM) --> Role Assignments
- Add the service principal `$Ngsa_AKS_Name` as "Reader"

#### Add client secret in the service principal
Add a secret to the service principal `$Ngsa_AKS_Name`
- Goto Azure Active Directory --> App Registration
- Search for `$Ngsa_AKS_Name` --> Goto `$Ngsa_AKS_Name`
- Goto Certificates and Secrets --> New Client Secret
- Note the SP Secret
- Note the App ID (from Overview)
- Note the Tenant ID (from Overview)

#### Add API Key to App Insights
For App Insights access, an API Key needs to be created.
- Goto  `$Ngsa_App_RG` resource group -->  `$Ngsa_Name` App Insights
- Under "API Access", create a new API Key with "Read Telemetry" access
- Note the API-Key

### Adding required permissions/secrets from Azure Cli
**It is recommended to use the Azure Portal**.

#### Add SP to Cosmos resoruce group
```bash
# Add ${Ngsa_AKS_Name} SP as "Reader" in ${Imdb_RG} resource group
az role assignment create --assignee $(az ad sp list --display-name "${Ngsa_AKS_Name}" --query "[].objectId" -o tsv) --role "Reader" --resource-group "${Imdb_RG}"
# Get the object id for SP: az ad sp list --display-name "${Ngsa_AKS_Name}" --query "[].objectId" -o tsv
```

#### Add client secret in the service principal
Add a secret to the service principal `$Ngsa_AKS_Name`
```bash
az ad sp credential reset --name $(az ad sp list --display-name "${Ngsa_AKS_Name}" --query '[].appId' -o tsv) --append --credential-description "grafana-cred"
# Sample output:
#{
#  "appId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
#  "name":  "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
#  "password": "THIS-IS-THE-CLIENT-SECRET",
#  "tenant":"XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
#}
```
Note the appId, password (client secret) and tenant

#### Add API Key to App Insights
```bash
az monitor app-insights api-key create --api-key grafana-access --read-properties ReadTelemetry --write-properties "" -g ${Ngsa_App_RG} --app ${Ngsa_Name}
# Note the "apiKey"
```

## Add Azure Monitor Source in Grafana
Get access to Grafana dashboard with the following command:
```bash
# It will port forward to localhost:PORT
istioctl dashboard grafana
```

In another terminal, get information on SP and App Insights:
```bash
# Get Directory(Tenant) ID, Application (Client) ID (our service principal)
# Get App-Insights AppID
az monitor app-insights component show -g kushalngsa-rg-app --query '[].appId' -o tsv
```
Goto a browser to access grafana and perform the following steps:
- Goto Configuration --> Data Sources
- "Add data source" --> Select Azure Monitor

# Resources
- [IaC-AKS readme - Deployment with Istio Servicemesh][1]
- [Azure Built-in Roles][2]


[1]: https://github.com/retaildevcrews/ngsa/tree/main/IaC/AKS
[2]: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles