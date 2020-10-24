# Naming Convention [Proposal]

## Common naming criteria for all resources
- Max length: `1-63` chars
- Start with lower case character, can’t end in `-` or `_` or any special char
> Preferred names have all lower case
>> A preferred name can be regexed by `[a-z]([-a-z0-9]*[a-z0-9])?`

## Azure resource names
Azure resources will have four parts:
- `[ prefix ]-( project-short-form )-[ identifier ]-[ suffix ]-( environment-type )`
  - Mandatory: `(project-short-form)`. Example: `ngsa`, `sp`
  - Mandatory: `(environment-type)` - usage env. Possible val: `prod`, `test` and `dev`
  - Optional: `[prefix]` or `[suffix]` - resource type suffix. Follows [Azure naming convention][1]
    - Common resource prefix/suffix:
        | Az Resource Name   | Prefix/Suffix |
        |--------------------|----------|
        | Key Vault          | kv-      |
        | Function           | func-    |
        | Virtual Machine    | vm-      |
        | VM Scale Set       | vmss-    |
        | CosmosDB           | cosmos-  |
        | AKS Cluster        | aks-     |
        | Container Registry | cr-      |
  - Optional: `[identifier]` - refers to descriptive label
  - Each part will be separated by a hyphen `-`
  - Resource names will use either suffix or prefix…

## Naming convention for Kubernetes resources 
### Label format

## Resourcese
- [Azure resource naming and tagging convention][1]
- [Azure resource name restrictions][2]

[1]: https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging
[2]: https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
