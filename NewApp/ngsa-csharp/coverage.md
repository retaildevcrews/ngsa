# Generate Code Coverage Report

## Start a Windows command prompt

> start in this directory `ngsa/NewApp/ngsa-csharp`

Run automated coverage report

> If your machine is slow, you may have to adjust the sleep times

``` cmd

run-coverage.cmd

```

Run manual coverage report

```cmd

rem you can ignore the global tool install error if already installed

set RUN_TEST_COVERAGE=true
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet build
cd Ngsa.LodeRunner

rem start the data service in test mode
start dotnet test ..\Ngsa.DataService.Tests\Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\

rem Wait for
rem    Starting test execution, please wait...
rem    A total of 1 test files matched the specified pattern.

rem run LodeRunner
dotnet run -- -s http://localhost:4122 -f dataservice.json

rem wait for data service test to finish (40 seconds or so)

rem start the data service
start dotnet run --project ..\Ngsa.Dataservice\Ngsa.DataService.csproj

rem run the app in test mode
rem   merge the results and create the coverage file
start dotnet test ..\Ngsa.App.Tests\Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover

rem Wait for
rem    Starting test execution, please wait...
rem    A total of 1 test files matched the specified pattern.

rem run LodeRunner
dotnet run -- -s http://localhost:4120 -f baseline.json

rem wait for app test to finish
rem press ctl-c stop data service

rem generate the coverage report
cd ..\TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html
rpt\index.html

```
