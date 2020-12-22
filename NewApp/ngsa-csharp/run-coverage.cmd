@echo off

rd TestResults /s/q

set RUN_TEST_COVERAGE=true
set IN_MEMORY=

dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet clean
dotnet build

start dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
timeout /T 5 /nobreak

start dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json
timeout /T 15 /nobreak

dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json

echo "done" > tests-complete
timeout /T 5 /nobreak
del tests-complete

set IN_MEMORY=true
start dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
timeout /T 10 /nobreak

cd Ngsa.LodeRunner
dotnet run -- -s localhost:4122 -f dataservice.json
cd ..

echo "done" > tests-complete
timeout /T 5 /nobreak
del tests-complete

set RUN_TEST_COVERAGE=
set IN_MEMORY=

dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover

cd TestResults
reportgenerator -reports:coverage.opencover.xml -targetdir:rpt -reporttypes:Html
rpt\index.html

cd ..
