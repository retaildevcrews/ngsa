# NGSA Application

## Overview

- There are 3 applications that make up the solution
- Ngsa.App
  - Web API
  - Requires Ngsa.DataService
- Ngsa.DataService
  - Data Service
    - Cosmos DB
    - in-memory (for testing)
- Ngsa.LodeRunner
  - End-to-end testing and load generation service
    - Can test Ngsa.App and/or Ngsa.DataService

### Pre-requisites

#### In-memory

- The in-memory data service requires no external dependencies as the data is loaded from data files in the project.

#### CosmosDB

> The files are mounted automatically by Kubernetes from the `ngsa-secrets` created during setup

- In order to run locally, you need to create 2 files in the src/app/secrets directory
  - CosmosUrl
  - CosmosKey

> The file names are case sensitive and are ignored in .gitignore

### Running NGSA Data Service

- Running In-memory

```bash

# from src/Ngsa.DataService
dotnet run -- --in-memory

```

- Running with CosmosDB:

```bash

# from src/Ngsa.DataService
dotnet run

```

### Verify the data service is running

```bash

curl http://localhost:4122/version

curl http://localhost:4122/api/genres

# from src/Ngsa.LodeRunner
dotnet run -- -s http://localhost:4122 -f dataservice.json

```

### Run the Web API

> The Web API requires the data service

```bash

# from src/Ngsa.App
dotnet run

```

### Verify the Web API is running

```bash

curl http://localhost:4120/version

curl http://localhost:4120/api/genres

# from src/Ngsa.LodeRunner
dotnet run -- -s http://localhost:4120 -f baseline.json

# longer end-to-end test
dotnet run -- -s http://localhost:4120 -f benchmark.json

```

### Run coverage tests

> Stop any running instances or the test will fail

```bash

# from src

# bash
./run-coverage.sh

# Windows
run-coverage.cmd

```

> Coverage results are available in `src/TestResults`
