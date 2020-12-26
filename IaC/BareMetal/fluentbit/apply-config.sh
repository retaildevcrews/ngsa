#!/bin/bash

envsubst < config.yaml | kubectl apply -f -