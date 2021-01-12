# NGSA Typescript Application

## Overview

- This project implements the Web API described in [/src/ngsa-csharp/README.md](/src/ngsa-csharp/README.md) in Node.js.
- It calls the [Ngsa.Dataservice](/src/ngsa-csharp/Ngsa.DataService) which either queries an in-memory database or a Cosmos DB instance.
- [Ngsa.LodeRunner](/src/ngsa-csharp/Ngsa.LodeRunner) is used for end-to-end testing and load generation.

## Pre-requisites

- Bash shell (tested on Visual Studio Codespaces, Mac, Ubuntu, Windows with WSL2)
  - Will not work with WSL1 or Cloud Shell
- Node.js 12.14.1+ ([download](https://nodejs.org/en/download/))
- npm 6.14.4+ (comes with Node.js)
- .NET Core SDK 3.1 ([download](https://dotnet.microsoft.com/download))
- Visual Studio Code (optional) ([download](https://code.visualstudio.com/download))

## Start the NGSA Data Service

Complete the steps in [Pre-requisites](/src/ngsa-csharp/README.md#pre-requisites), [Running NGSA Data Service](/src/ngsa-csharp/README.md#running-ngsa-data-service), and [Verify the data service is running](/src/ngsa-csharp/README.md#verify-the-data-service-is-running).

## Run the Web API

> This will work from a terminal in Visual Studio Codespaces as well

```bash

# install modules in package.json file
# note: you may see output like the following, this is expected and safe to ignore
# npm WARN optional SKIPPING OPTIONAL DEPENDENCY: fsevents@2.1.3 (node_modules/chokidar/node_modules/fsevents):
# npm WARN notsup SKIPPING OPTIONAL DEPENDENCY: Unsupported platform for fsevents@2.1.3: wanted {"os":"darwin","arch":"any"} (current: {"os":"linux","arch":"x64"})
# npm WARN optional SKIPPING OPTIONAL DEPENDENCY: fsevents@1.2.13 (node_modules/fsevents):
# npm WARN notsup SKIPPING OPTIONAL DEPENDENCY: Unsupported platform for fsevents@1.2.13: wanted {"os":"darwin","arch":"any"} (current: {"os":"linux","arch":"x64"})

npm install

# build the app

npm run build

# run the app

npm start

# optionally, set the logging level verboseness with --log-level (or -l)
# 'info' is the default

npm start -- --log-level warn

# alternatively you can set the following environment variables and run without command line args

export LOG_LEVEL=info # (optional)

npm start

# wait until 'Server is listening on port 4120' is displayed

```

## Verify the Web API is running

Open a new bash shell

> Visual Studio Codespaces allows you to open multiple shells by clicking on the `Split Terminal` icon

```bash

curl http://localhost:4120/version

curl http://localhost:4120/api/genres

# from src/ngsa-csharp/Ngsa.LodeRunner
dotnet run -- -s http://localhost:4120 -f baseline.json

# longer end-to-end test
dotnet run -- -s http://localhost:4120 -f benchmark.json

```

## Run coverage tests

> TODO: Implement similar coverage tests as csharp

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [Microsoft Contributor License Agreement](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
