# Generate Code Coverage Report

## Start three Windows command prompts

- to run in bash
  - change `\` to `/`
  - change `set` to `export`


```bash

# t1
cd ngsa\NewApp\ngsa-csharp
set RUN_TEST_COVERAGE=true
dotnet tool install -g dotnet-reportgenerator-globaltool

# t2
cd ngsa\NewApp\ngsa-csharp\Ngsa.LodeRunner
dotnet build

# t3
cd ngsa\NewApp\ngsa-csharp\Ngsa.DataService

# t1 - start the data service in test mode
dotnet test Ngsa.DataService.Tests\Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput="../TestResults/"

# Wait for
#    Starting test execution, please wait...
#    A total of 1 test files matched the specified pattern.

# t2 - run LodeRunner
dotnet run -- -s http://localhost:4122 -f dataservice.json

# wait for t1 to finish (45 seconds or so)

# t3 - start the data service
dotnet run

# t1 - run the app in test mode
# merge the results and create the coverage file
dotnet test Ngsa.App.Tests\Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput="../TestResults/" /p:MergeWith="../TestResults/coverage.json" /p:CoverletOutputFormat="opencover"

# Wait for
#    Starting test execution, please wait...
#    A total of 1 test files matched the specified pattern.

# t2 - LodeRunner
dotnet run -- -s http://localhost:4120 -f baseline.json

# wait for t1 to finish
# press ctl-c in t2 to stop data service

# t1

# generate the coverage report
cd TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html

# open coverage report in your browser
cd rpt
index.html

```
