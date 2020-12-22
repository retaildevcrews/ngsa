#!/bin/bash

rm -rf TestResults

export RUN_TEST_COVERAGE=true
export IN_MEMORY=

dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet clean
dotnet build

nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 5

nohup dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json &
sleep 15

dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
sleep 30

set IN_MEMORY=true
nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 10

cd Ngsa.LodeRunner
dotnet run -- -s localhost:4122 -f dataservice.json
cd ..
sleep 15

unset RUN_TEST_COVERAGE
unset IN_MEMORY

dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover
