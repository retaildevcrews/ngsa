# Developer Roadmap

- TODO - per review feedback, this should be a "roadmap"
  - this is where a dev would start
  - explain ngsa and loderunner
  - link to how to deploy to k8s

- TODO - how "hierarchical" should the docs be?
  - is it better to have shorter docs in each directory?

## Source Code Hierarchy

- ngsa-typescript
  - typescript version of the ngsa application
- ngsa-csharp
  - C# version of the ngsa application
  - DataService (required by ngsa app)
  - LodeRunner (load service)

## Running the Applications

1. To reduce setup complexity we suggest that you follow the directions for running the application in In-memory mode as detailed in the [NGSA App Readme](./ngsa-csharp/README.md)
2. Once the app is running locally you may run tests against it using [Loderunner](https://github.com/retaildevcrews/ngsa/blob/main/src/loderunner/README.md./loderunner/README.md)
