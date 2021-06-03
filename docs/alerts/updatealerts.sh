#! /bin/bash

for filename in ./*.json; do
  # get name of alert
  export Alert_Name=`echo $(basename "$filename" .json)`

  # update environment specific variables in temporary json file
  cp $filename $filename-temp.json
  sed -i "s/%%SUBSCRIPTION_GUID%%/$(eval $Ngsa_Sub)/g" $filename-temp.json
  sed -i "s/%%Ngsa_Log_Analytics_RG%%/${Ngsa_Log_Analytics_RG}/g" $filename-temp.json
  sed -i "s/%%Ngsa_Log_Analytics_Name%%/${Ngsa_Log_Analytics_Name}/g" $filename-temp.json
  sed -i "s/%%Ngsa_Action_Group_Name%%/${Ngsa_Action_Group_Name}/g" $filename-temp.json

  # create or update alert
  az rest --method PUT --url "https://management.azure.com/subscriptions/$(eval ${Ngsa_Sub})/resourceGroups/${Ngsa_Log_Analytics_RG}/providers/microsoft.insights/scheduledqueryrules/${Alert_Name}?api-version=2018-04-16" --body @$filename-temp.json &> /dev/null

  # remove temporary file
  rm $filename-temp.json

  # output status
  echo "Created/updated $Alert_Name."
done
