#!/bin/sh

export RUN_TEST_COVERAGE=true
export DATA_SERVICE=http://ds:4122

dotnet test /p:CollectCoverage=true /p:CoverletOutput="./TestResults/" $CONFIGURATION
