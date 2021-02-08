# NGSA GitOps for Continuous Deployment
![License](https://img.shields.io/badge/license-MIT-green.svg)


The following instructions will set up Flux in a Kubernetes cluster for continuous deployment of the NGSA application.

### Prerequisites

- Kubernetes cluster
- Helm v3
- [fluxctl](https://docs.fluxcd.io/en/1.18.0/references/fluxctl.html)


### Installation Instructions

Add FluxCD repository to Helm repos

```bash

helm repo add fluxcd https://charts.fluxcd.io

```

Create the namespace used by Flux

``` bash

kubectl create ns fluxcd

```

Install Flux Helm chart. Make sure you are using the right values depending on the desired configuration.
```bash

# Set your target git url
# example: export Git_Url=git@github.com:retaildevcrews/ngsa.git
export Git_Url=[your git url]

# Set your target git branch. This is the branch that Flux will track.
# example: export Git_Branch=main
export Git_Branch=[your git branch]

# Set your target directory git path. This is the path Flux will look for yaml to be applied in the cluster.
# example: export Git_Path=ngsa-cd/deployments/preprod/central\\,ngsa-cd/deployments/preprod/common
export Git_Path=[your git path]


helm upgrade -i flux fluxcd/flux --wait \
  --namespace fluxcd \
  --set git.url=$Git_Url \
  --set git.branch=$Git_Branch \
  --set git.path=$Git_Path \
  --set additionalArgs={--sync-garbage-collection}

```

Install the `HelmRelease` Kubernetes custom resource definition
```bash

kubectl apply -f https://raw.githubusercontent.com/fluxcd/helm-operator/master/deploy/crds.yaml

```
Install the Flux Helm Operator
``` bash

helm upgrade -i helm-operator fluxcd/helm-operator --wait \
--namespace fluxcd \
--set git.ssh.secretName=flux-git-deploy \
--set helm.versions=v3

```

Store the following key in GitHub as a deployment key. Make sure allow write-access is enabled.

 ``` bash

fluxctl identity --k8s-fwd-ns fluxcd

```

Your cluster should now sync with your configuration stored in GitHub. By default, the cluster looks for changes in the cluster every five minutes. To force a sync, use the following command.
``` bash

fluxctl sync --k8s-fwd-ns fluxcd

```

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [Microsoft Contributor License Agreement](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments
