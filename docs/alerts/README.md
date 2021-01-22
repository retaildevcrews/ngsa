# Create NGSA Pre-prod Environment Alerts

The JSON files in this directory contain the properties for the alerts described in [Alerts.md](../Alerts.md).  The `updatealerts.sh` script can be used to quickly create or update all of the alerts using the Azure CLI instead of the portal. The JSON file properties can be updated with different configuration values to update the corresponding alerts.  It is recommended to test out any query updates in Log Analytics before updating the alert to ensure the query is valid and returns expected results.

## Run script to create or update NGSA Pre-prod Alerts

### Set required environment variables (if not set already)

```bash

# get subscription id
export Ngsa_Sub='az account show -o tsv --query id'

# set prefix
export Ngsa_Name=ngsa-pre-west

# set name of existing resource group containing log analytics
export Ngsa_Log_Analytics_RG="${Ngsa_Name}-log-rg"

# set name of exitsting log analytics instance
export Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"

```

### Create Action Group

```bash

# set name of action group for alerts
export Ngsa_Action_Group_Name="${Ngsa_Name}-action-group"

# create the action group (if it doesnt exist already)
az monitor action-group create --name $Ngsa_Action_Group_Name --resource-group $Ngsa_Log_Analytics_RG --action email {Name} {email address}

# update the group with as many email addresses as required (optional)
az monitor action-group update -n $Ngsa_Action_Group_Name -g $Ngsa_Log_Analytics_RG --add-action email {Name} {email address}

```

### Run the script

```bash

# run script to update or create alerts
./updatealerts.sh


```
