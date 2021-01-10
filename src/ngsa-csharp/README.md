# NGSA Application

## Overview

- describe app
- describe modalities (in-memory, CosmosDB)

### Pre-requisites

#### In-memory

The in-memory execution requires no external dependencies as the data served back is loaded from static files in the project.

#### CosmosDB

- In order to run locally, you need to create 2 files in the src/app/secrets directory
  - CosmosUrl
  - CosmosKey

> The file names are case sensitive and are ignored in .gitignore

### Running NGSA Locally

- Running In-memory

```bash

# from src/ngsa-csharp/Ngsa.App
dotnet run -- --in-memory

```

- Running with CosmosDB:

```bash

# from src/ngsa-csharp/Ngsa.App
dotnet run

```

### Run tests

```bash

# from src/ngsa-csharp/Ngsa.App.Tests
dotnet test

```

### Run coverage tests

```bash

# from src/ngsa-csharp

# bash
./run-coverage.sh

# Windows
run-coverage.cmd

```

> Coverage results are available in `src/ngsa-csharp/TestResults`
