@echo off

set RUN_TEST_COVERAGE=true
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet build
cd Ngsa.LodeRunner

start dotnet test ..\Ngsa.DataService.Tests\Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\

timeout /T 25 /nobreak

dotnet run -- -s http://localhost:4122 -f dataservice.json

timeout /T 30 /nobreak

start dotnet run --project ..\Ngsa.Dataservice\Ngsa.DataService.csproj

start dotnet test ..\Ngsa.App.Tests\Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover

timeout /T 20 /nobreak

dotnet run -- -s http://localhost:4120 -f baseline.json

timeout /T 25 /nobreak

cd ..\TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html
rpt\index.html

cd ..
