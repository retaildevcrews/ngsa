# NGSA

Next Generation Symmetric Apps

![License](https://img.shields.io/badge/license-MIT-green.svg)

## Project Description

Understanding how infrastructure changes will impact application capabilities.  To that end, the NGSA effort is focused on creating a Kubernetes based application host environment in conjunction with a platform validation app that will allow continuous monitoring of the impact of changes in the hosting environment based on performance metrics results from a representative workload.

At it's culmination the project will be a collection of guidance for deployment topologies based on capability maturity, a platform validation application, and a load generation tool.

### Table of Contents

1. [Getting Started](#getting-started)
2. [Contributing](#contributing)
3. Cluster Setup Guides
   1. [Developer Cluster](./IaC/DevCluster)
   2. [AKS](./IaC/AKS)
4. [Developer Guide](./NewApp/ngsa-csharp)
5. Applications
   1. [Web API](./NewApp/ngsa-csharp)
   2. [Data Service](./NewApp/ngsa-csharp)
   3. [Load Generator](./NewApp/ngsa-csharp)
6. Design, Practices, and Conventions
   1. [Architecture](./docs/ApplicationArch.md)
   2. [Engineering Practices](./docs/EngineeringPractices.md)
      1. [Naming Conventions](./docs/NamingConvention.md)
      2. [CI/CD](./docs/CICD.md)
      3. [Observability](./docs/Observability.md)
   3. Web API Specification
      1. [Parameter Validation](./docs/ParameterValidation.md)
      2. [HTTP Error Responses](./docs/HttpErrorResponses.md)

### Getting Started

The NGSA solution is a combination of a Kubernetes based infrastructure and a `Platform Validation Application`.

We suggest that you follow these steps to get started:

1. [Run the app locally](./NewApp/ngsa-csharp)
2. [Setup local Dev Cluster](./IaC/DevCluster)
3. [Deploy to Dev Cluster](./Iac/DevCluster)

### Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Trademarks

This project may contain trademarks or logos for projects, products, or services.

Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).

Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.

Any use of third-party trademarks or logos are subject to those third-party's policies.
