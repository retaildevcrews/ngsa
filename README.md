# NGSA

> Next Generation Symmetric Apps

## Projection Description

Understanding how infrastructure changes will impact application capabilities.  To that end, the NGSA effort is focused on creating a Kubernetes based application host environment in conjunction with a platform validation app that will allow continuous monitoring of the impact of changes in the hosting environment based on performance metrics results from a representative workload.

At it's culmination the project will be a collection of guidance for deployment topologies based on capability maturity, a platform validation application, and a load generation tool.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Contributing](#contributing)
3. Cluster Setup Guides
   1. [Bare Metal](./IaC/BareMetal/README.md)
   2. [AKS](./IaC/AKS/README.md)
4. [Developer Guide](./docs/Developer.md)
5. Applications
   1. [NGSA App](./src/ngsa/README.md)
   2. [Load Generator](./src/loderunner/README.md)
6. Design, Practices, and Conventions
   1. [Architecture](./docs/ApplicationArch.md)
   2. [CI/CD](./docs/CICD.md)
   3. [HTTP Error Responses](./docs/HttpErrorResponses.md)
   4. [Engineering Practices](./docs/EngineeringPractices.md)
   5. [Observability](./docs/Observability.md)
   6. [Parameter Validation](./docs/ParameterValidation.md)
   7. [Naming Conventions](./docs/NamingConvention.md)

![License](https://img.shields.io/badge/license-MIT-green.svg)

## Getting Started

[WIP] Add descriptive text to guide through cluster setup, app setup, and load execution.

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
