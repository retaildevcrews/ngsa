# Create alerts using az rest

```bash

# get subscription id
export Ngsa_Sub='az account show -o tsv --query id'

# set prefix
export Ngsa_Name=ngsa-pre-west

# set name of existing resource group containing log analytics
export Ngsa_Log_Analytics_RG="${Ngsa_Name}-log-rg"

# set name of exitsting log analytics instance
export Ngsa_Log_Analytics_Name="${Ngsa_Name}-log"

# set name of action group for alerts
export Ngsa_Action_Group_Name="${Ngsa_Name}-action-group"

# create the action group (if it doesnt exist already)
az monitor action-group create --name $Ngsa_Action_Group_Name --resource-group $Ngsa_Log_Analytics_RG --action email {Name} {email address}

# update the group with as many email addresses as required (optional)
az monitor action-group update -n $Ngsa_Action_Group_Name -g $Ngsa_Log_Analytics_RG --add-action email {Name} {email address}

# run script to update or create alerts
./updatealerts.sh

```
