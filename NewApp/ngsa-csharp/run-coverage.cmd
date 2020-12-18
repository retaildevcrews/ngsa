@echo off

set RUN_TEST_COVERAGE=true
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet clean
dotnet build
cd Ngsa.LodeRunner

start dotnet test ..\Ngsa.App.Tests\Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\ /p:MergeWith=..\TestResults\coverage.json
timeout /T 10 /nobreak

start dotnet test ..\Ngsa.DataService.Tests\Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=..\TestResults\  /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover
timeout /T 15 /nobreak

dotnet run -- -s http://localhost:4120 -f baseline.json
timeout /T 25 /nobreak

cd ..\TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html
rpt\index.html

cd ..
