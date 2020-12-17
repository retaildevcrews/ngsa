#!/bin/sh

export RUN_TEST_COVERAGE=true

if [ ! -f ../Ngsa.DataService/secrets/CosmosKey ]
then
  export IN_MEMORY=true
fi

dotnet test /p:CollectCoverage=true /p:CoverletOutput="./TestResults/" $CONFIGURATION
