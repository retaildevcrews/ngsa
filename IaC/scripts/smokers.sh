#!/bin/bash
while getopts :n:r:s: option
do
 case "${option}" in
 n) NGSA_NAME=${OPTARG};;
 r) NGSA_SMOKER_RG=${OPTARG};;
 s) NGSA_ENDPOINT=${OPTARG};;
 *) echo "Please refer to usage guide on GitHub" >&2
    exit 1 ;;
 esac
done

export logId='az monitor log-analytics workspace show -g $NGSA_SMOKER_RG -n $NGSA_NAME --query customerId -o tsv'
export logKey='az monitor log-analytics workspace get-shared-keys -g $NGSA_SMOKER_RG -n $NGSA_NAME --query primarySharedKey -o tsv'

WEBV_SLEEP=1000
for l in 'westus2' 'eastus2' 'westeurope' 'japaneast'
do
  az container delete -y  -g $NGSA_SMOKER_RG --query name -o tsv -n $NGSA_NAME-webv-${l}

  az container create --subscription bartr-wcnp -g $NGSA_SMOKER_RG --image retaildevcrew/webvalidate:beta -o tsv --query name \
  -n $NGSA_NAME-webv-${l} -l ${l} \
  --log-analytics-workspace $(eval $logId) --log-analytics-workspace-key $(eval $logKey) \
  --command-line "dotnet ../webvalidate.dll --tag ${l} -l ${WEBV_SLEEP} -s $NGSA_ENDPOINT -u https://raw.githubusercontent.com/retaildevcrews/ngsa/main/TestFiles/ -f benchmark.json -r --summary-minutes 5 --json-log"

  # order matters!
  WEBV_SLEEP=10000

  az container logs -g $NGSA_SMOKER_RG -n $NGSA_NAME-webv-${l}
done

