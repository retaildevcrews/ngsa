#!/bin/bash

if [ ! -f "./Ngsa.DataService/secrets/CosmosKey" ]; then
  echo "Error: Ngsa.DataService/secrets/CosmosKey does not exist"
  exit 1
fi

# install coverage tool
# ignore already installed error
dotnet tool install -g dotnet-reportgenerator-globaltool

# build
dotnet clean
dotnet build

# remove existing test results
rm -rf TestResults
rm tests-complete

# set environment variables
export RUN_TEST_COVERAGE=true
export IN_MEMORY=true

# run in memory tests
nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 5

nohup dotnet test Ngsa.App.Tests/Ngsa.App.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/ /p:MergeWith=../TestResults/coverage.json &
sleep 10

dotnet test Ngsa.LodeRunner.Tests/Ngsa.LodeRunner.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json

# signal tests with file semaphore
touch tests-complete
sleep 3
rm tests-complete

# run Cosmos tests
unset IN_MEMORY
nohup dotnet test Ngsa.DataService.Tests/Ngsa.DataService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=../TestResults/  /p:MergeWith=../TestResults/coverage.json &
sleep 15

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
