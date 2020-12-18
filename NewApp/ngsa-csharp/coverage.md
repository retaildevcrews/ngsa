# Generate Code Coverage Report

## Start a Windows command prompt

> start in this directory `ngsa/NewApp/ngsa-csharp`

Run automated coverage report

> If your machine is slow, you may have to adjust the sleep times

``` cmd

run-coverage.cmd

```

Run manual coverage report

```bash

# you can ignore the global tool install error if already installed

set RUN_TEST_COVERAGE=true
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet clean
dotnet build
cd Ngsa.LodeRunner

# start app in test mode
start dotnet test ..\Ngsa.App.Tests\Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\ /p:MergeWith=..\TestResults\coverage.json

# Wait for
#    Starting test execution, please wait...

# start data service in test mode
start dotnet test ..\Ngsa.DataService.Tests\Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\  /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover

# Wait for
#    Starting test execution, please wait...

# run LodeRunner
dotnet run -- -s http://localhost:4120 -f baseline.json

# wait for services to finish (30 seconds or so)

# generate the coverage report
cd ..\TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html
rpt\index.html

```
