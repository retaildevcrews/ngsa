#!/bin/bash

# remove existing test results
rm -rf TestResults

# install coverage tool
# ignore already installed error
dotnet tool install -g dotnet-reportgenerator-globaltool

# build
dotnet clean
dotnet build

# set environment variables
export RUN_TEST_COVERAGE=true
unset IN_MEMORY=

# run Cosmos DB tests
nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 5

nohup dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json &
sleep 15

dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json

# signal tests with file semaphore
touch tests-complete
sleep 3
rm tests-complete

# run in memory tests
set IN_MEMORY=true
nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 10

cd Ngsa.LodeRunner
dotnet run -- -s localhost:4122 -f dataservice.json
cd ..

# signal tests with file semaphore
touch tests-complete
sleep 3
rm tests-complete

# unset env vars
unset RUN_TEST_COVERAGE
unset IN_MEMORY

# run unit tests
dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json
dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json /p:CoverletOutputFormat=opencover

# remove output
rm nohup.out
