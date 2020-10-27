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
  - Optional: `[prefix]` or `[suffix]` - resource type suffix.
  - It follows [Azure naming convention][1]
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
Naming convention applies to kubernetes labels and namespaces.
### Labels
A kubernetes label will have three portions. Label prefix, label name and label value.
Length of each portion should conform to [common naming criteria](#common-naming-criteria-for-all-resources)

In addition to using custom labels, [labels recommended by kubernetes][3] should be put to use as much as possible.

This guideline is loosely adhering to ["Kubernetes common naming constraints"][4].

#### Label prefix
- 
- Label prefix could be
  - *a DNS subdomain*
  - *System-type-name*
  - *Project prefix etc*
- Label prefixes should end with "/"
- Label prefix is is optional
  - But recommended for autonomous processes
  - Custom user-labels doesn't need to conform to this

#### Label name
- Label name is mandatory
- Only alphanumeric, "-", "_" or "." are permitted
  - Alphabetic characters should be lower-cased
- Label names should begin and end with an alphanumeric character

#### Label value
- Could be anything
- If required we can pose a restriction or convension to values


### Namespaces
Currently kubernetes doesn't have any recommendation for namespace format.

But the community braodly follows "*system*-*type*" format for namespaces.
For example:
- Namespaces created by kubernetes: `kube-node-lease`, `kube-system` and `kube-public`
- istio creates two namespaces: `istio-system` and `istio-operator`

## Resourcese
- [Azure resource naming and tagging convention][1]
- [Azure resource name restrictions][2]
- [Kubernetes recommended Label][3]
- [Kubernetes common naming constraints][4]


[1]: https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging
[2]: https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
[3]: https://kubernetes.io/docs/concepts/overview/working-with-objects/common-labels/
[4]: https://kubernetes.io/docs/concepts/overview/working-with-objects/names/