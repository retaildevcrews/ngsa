# Developer Docs

- TODO - explain why - k8s secrets are mounted

- In order to run locally, you need to create 2 files in the src/app/secrets directory
  - CosmosUrl
  - CosmosKey

> The file names are case sensitive and are ignored in .gitignore

## Run locally

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
