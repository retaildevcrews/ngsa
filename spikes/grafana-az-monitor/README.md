# Adding Azure Monitor source in Grafana

The following instructions allow adding Azure Monitor plugin as a source and create custom Grafana dashboards in an existing Istio setup.

*Warning:*  These instructions will install the AZ Monitor plugin manually inside the grafana pod. This is only intended for spike purposes and should not be used in a production environment. If the pod were to be deleted, the Grafana deployment will spin a new pod and the existing configuration will be lost. Use at your own risk.

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

#### Add SP to cosmos resource group
Add `$Ngsa_AKS_Name` SP to the cosmos resource group `$Imdb_RG`:
- Go to resource group `$Imdb_RG` --> Access Control (IAM) --> Role Assignments
- Add the service principal `$Ngsa_AKS_Name` as "Reader"

#### Add client secret in the service principal
Add a secret to the service principal `$Ngsa_AKS_Name`
- Goto Azure Active Directory --> App Registration
- Search for `$Ngsa_AKS_Name` --> Goto `$Ngsa_AKS_Name`
- Goto Certificates and Secrets --> New Client Secret
- *Note the SP Secret, App ID (from Overview) and the Tenant ID (from Overview)*

#### Add API Key to App Insights
For App Insights access, an API Key needs to be created.
- Goto  `$Ngsa_App_RG` resource group -->  `$Ngsa_Name` App Insights
- Under "API Access", create a new API Key with "Read Telemetry" access
- *Note the API-Key*

### Adding required permissions/secrets from Azure Cli
**It is recommended to use the Azure Portal**.

#### Add SP to Cosmos resource group
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
*Note the appId, password (client secret) and tenant*

#### Add API Key to App Insights
```bash
az monitor app-insights api-key create --api-key grafana-access --read-properties ReadTelemetry --write-properties "" -g ${Ngsa_App_RG} --app ${Ngsa_Name}
# Sample output
#{
#  "apiKey": "API-KEY",
#  "createdDate": "Wed, 28 Oct 2020 16:42:18 GMT",
#  ....
#  "name": "grafana-access",
#  "resourceGroup": "NAME-OF-RESOURCE-GROUP"
#}
az monitor app-insights component show -g ${Ngsa_App_RG} --query '[].appId'
# Sample output:
# [ "eedbf8f0-4a82-4644-9194-bbdf433b9354" ] --> This is the insights appId
```
*Note the "apiKey" and the insights appId*

## Add Azure Monitor Source in Grafana
Get access to Grafana dashboard with the following command:
```bash
# It will port forward to localhost:PORT
istioctl dashboard grafana
```

Goto a browser to access grafana and perform the following steps:
- Goto Configuration --> Data Sources
- "Add data source" --> Select "Azure Monitor"
- Inside "Azure Monitor" Source
  - Under Azure Monitor Details
    - Put in Directory (Tenant) ID, Application (Client) ID (service principal `$Ngsa_AKS_Name` ID) and client secret from [this step](#add-client-secret-in-the-service-principal)
    - Click on "Load Subscription" --> After loading, select proper subscription from drop-down
  - Under Application Insights
    - Put in "API Key" and "Application ID" from [this step](#add-api-key-to-app-insights)
  - Click "Save & Test"
- Click on "Explore" from Grafana side bar
- Try out different metrics and services

# Resources
- [IaC-AKS readme - Deployment with Istio Servicemesh][1]
- [Azure Built-in Roles][2]

[1]: https://github.com/retaildevcrews/ngsa/tree/main/IaC/AKS
[2]: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
