# NGSA Application

[TODO - fix run commands, replace webv with loderunner]

## Overview

- describe app
- describe modalities (in-memory, CosmosDB)

## Pre-requisites

### In-memory

The in-memory execution requires no external dependencies as the data served back is loaded from static files in the project.

### CosmosDB

- In order to run locally, you need to create 3 files in the src/app/secrets directory
  - CosmosUrl
  - CosmosKey
  - AppInsightsKey (optional)

> The file names are case sensitive and are ignored in .gitignore

## Running NGSA Locally

**Option 1** - Starting In-memory:

```bash

# from src/app
dotnet run

```

**Option 2** - Starting with CosmosDB:

```bash

# from src/app
dotnet run

```

## Run tests

```bash

# from src/tests
dotnet test

```

- You can run a complete end-to-end test using webv
  - You must create your secrets first
  - You will need a second bash console.

```bash

# from src/tests (first console)
./runtests

# wait for Application started. Press Ctrl+c to shutdown.

# from TestFiles (second console)
webv -s localhost:4120 -f baseline.json

```

> Coverage results are available in `src/tests/TestResults`
