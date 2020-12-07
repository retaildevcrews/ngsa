# Node.js and Restify Web API application

> Build a Node.js and Restify Web API application that calls a data service running on localhost:4122

## Prerequisites

- Bash shell (tested on Visual Studio Codespaces, Mac, Ubuntu, Windows with WSL2)
  - Will not work with WSL1 or Cloud Shell
- Node.js 12.14.1+ ([download](https://nodejs.org/en/download/))
- npm 6.14.4+ (comes with Node.js)
- Visual Studio Code (optional) ([download](https://code.visualstudio.com/download))

## Setup

### Make sure the data service is running on localhost:4122

> TODO: Add instructions

### Using bash shell

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

npm start -- --log-level

# alternatively you can set the following environment variables and run without command line args

export LOG_LEVEL=info # (optional)

npm start

```

wait for `Server is listening on port 4120`

### Testing the application

Open a new bash shell

> Visual Studio Codespaces allows you to open multiple shells by clicking on the `Split Terminal` icon

```bash

# test the application

# test using httpie (installed automatically in Codespaces)
http localhost:4120/version

# test using curl
curl localhost:4120/version

```

Stop helium by typing Ctrl-C or the stop button if run via F5

### Deep Testing

We use [Web Validate](https://github.com/Microsoft/webvalidate) to run deep verification tests on the Web API

If you have dotnet core sdk installed

```bash

# install Web Validate as a dotnet global tool
# this is automatically installed in CodeSpaces
dotnet tool install -g webvalidate

# make sure you are in the root of the repository

# run the validation tests
# validation tests are located in the TestFiles directory
cd TestFiles

webv -s localhost:4120 -f baseline.json

# there may be a validation error on the /healthz/ietf endpoint test
#   json: status: warn : Expected: pass
# the "warn" status indicates a slower than normal response time
# this is OK and will occasionally occur

# benchmark.json is a 300 request test that covers the entire API

# cd to root of repo
cd ..

```

Test using Docker image

```bash

# make sure you are in the root of the repository

# run the validation tests
# mount the local TestFiles directory to /app/TestFiles in the container
# 172.17.0.1 is the docker host IP
docker run -it --rm -v TestFiles:/app/TestFiles retaildevcrew/webvalidate -s http://172.17.0.1:4120 -f baseline.json

# there may be a validation error on the /healthz/ietf endpoint test
#   json: status: warn : Expected: pass
# the "warn" status indicates a slower than normal response time
# this is OK and will occasionally occur

# benchmark.json is a 300 request test that covers the entire API

```

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
