# TODO: title

Rough notes

## Prerequisites

- follow aks-secure-baseline walkthrough

## Infrastructure notes

- Hub network from walkthrough is in eastus2
- First spoke network from walkthrough is in eastus2
- First AKS cluster from walkthrough is in the eastus2 spoke
- `rg-bu0001a0008` resource group is for the application team
  - bu0001 is for Business Unit 0001
  - 0008 is the application ID
- flux is installed in the cluster as part of the walkthrough
  - it is in the `cluster-baseline-settings` namespace

## Decisions & Questions

### resource groups

Using the existing resource groups for this spike.

```txt

For NGSA, we'll use `ngsa-pre-shared-rg`, and `ngsa-pre-app-rg`.

```

### cosmos

Create cosmos in rg-bu0001a0008. Using the first spoke network for simplicity because it is already there. This will be done through the portal so cosmos can be in centralus.

```txt

For NGSA, cosmos is in centralus.

When a new spoke network is created for centralus, create cosmos there using the cli.

Investigate network configurations for cosmos? Using the default "All networks" option for this spike.

```

### flux

The `cluster-manifests` directory was copied as-is from aks-secure-baseline.

```txt

cluster-manifests/cluster-baseline-settings/flux.yaml was edited to point this spike.

NGSA is using helm to deploy flux. Check that settings in aks-secure-baseline are in sync with helm settings during install.

```

## Commands

Create cosmos

```bash

export Imdb_Name="ngsa-pnp-spike-cosmos"
export Imdb_RG="rg-bu0001a0008"
export Imdb_DB=imdb
export Imdb_Col=movies
export Imdb_RW_Key='az cosmosdb keys list -n $Imdb_Name -g $Imdb_RG --query primaryMasterKey -o tsv'

# az cosmosdb create -g $Imdb_RG -n $Imdb_Name
az cosmosdb sql database create -a $Imdb_Name -n $Imdb_DB -g $Imdb_RG --throughput 1000
az cosmosdb sql container create -p /partitionKey -g $Imdb_RG -a $Imdb_Name -d $Imdb_DB -n $Imdb_Col

docker run -it --rm retaildevcrew/imdb-import $Imdb_Name $(eval $Imdb_RW_Key) $Imdb_DB $Imdb_Col

```

Flux setup

```bash

az aks get-credentials --resource-group rg-bu0001a0008 --name aks-ioxqpbmcqokqq

```
