# TLS certificates with Let's Encrypt and cert-manager

Setup TLS certificate from Let's Encrypt using cert-manager.

Before starting this spike, follow the instructions [here](../../IaC/AKS/README.md) to setup the NGSA application in AKS.

## DNS, SSL/TLS Prerequisites

 A domain name and SSL/TLS certificates are required for HTTPS access over the internet.

- Registered domain with permissions to update nameservers
- Azure subscription with permissions to create a DNS Zone

## Note on Rate Limits

>[Let's Encrypt](https://letsencrypt.org/) is used to issue TLS certificates.  Let's Encrypt has [rate limit policies](https://letsencrypt.org/docs/rate-limits/) that could be triggered if you run multiple deployments in sequence.  Please take note and be careful not to exceed their rate thresholds.

## Set variables

```bash

# Set email to register with Let's Encrypt
export Ngsa_Email=[your email address]

# Set your registered domain name.
# example: export Ngsa_Domain_Name=cse.ms
export Ngsa_Domain_Name=[your domain name]

# DNS name
export Ngsa_App_Endpoint="${Ngsa_Name}.${Ngsa_Domain_Name}"

```

## Setup SSL/DNS

> Note: A registered domain name is required for this section.

### DNS Setup

Create a DNS A record mapping your domain to the Istio ingress gateway IP address.

This is a setup using Azure DNS. In this setup, update your domain to use Azure DNS Zone nameservers.

```bash

# example: export Ngsa_DNS_RG=dns-rg
export Ngsa_DNS_RG=[dns resource group name]

# Check if DNS resource group exists
az group exists -n $Ngsa_DNS_RG

# If false, create DNS resource group
az group create -n $Ngsa_DNS_RG -l $Ngsa_Location

# Check if DNS Zone exists
az network dns zone show --name $Ngsa_Domain_Name -g $Ngsa_DNS_RG -o table

# If not found, create the DNS Zone.
az network dns zone create -g $Ngsa_DNS_RG -n $Ngsa_Domain_Name

# Add DNS A record for the Istio ingress gateway.
az network dns record-set a add-record -g $Ngsa_DNS_RG -z $Ngsa_Domain_Name -n $Ngsa_Name -a $INGRESS_PIP

# Show the Azure nameservers for your DNS Zone.
az network dns zone show -n $Ngsa_Domain_Name -g $Ngsa_DNS_RG --query nameServers -o tsv

# Update your domain to use the result entries for nameservers.

```

### Install Cert-Manager

```bash

cd $REPO_ROOT/IaC/AKS/cluster/manifests/cert-manager

export CERT_MANAGER_VERSION=1.0.3
kubectl create namespace cert-manager
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --version "v${CERT_MANAGER_VERSION}" \
  --set installCRDs=true

# wait for the cert manager pods to be ready
kubectl get pods --namespace cert-manager

# Create a staging and production ClusterIssuer for cert-manager
# Use the staging ClusterIssuer for testing. Once ready, use the production resource.
envsubst < clusterissuer.yaml | kubectl apply -f -

```

## Add Let's Encrypt staging certs

Create the staging certificate and gateway.

```bash

cd $REPO_ROOT/spikes/lets-encrypt

envsubst < staging-gateway.yaml | kubectl apply -n ngsa -f -

envsubst < staging-certificate.yaml |  kubectl apply -n istio-system -f -

```

Update the host field in the helm values file.

```bash

cd $CHART_REPO/charts/ngsa

# Use the helm-config.yaml file to configure the deployment
envsubst < helm-config.example.yaml > helm-config.yaml

```

The `helm-config.yaml` file can be used as an override to the default values during the helm install.

```bash

cd $CHART_REPO/charts/

# Install NGSA using the upstream ngsa image from Dockerhub
# Start by using the "letsencrypt-staging" ClusterIssuer to get test certs from the Let's Encrypt staging environment.
helm upgrade ngsa-aks ngsa -f ./ngsa/helm-config.yaml --namespace ngsa --set cert.enabled=false --set gateway.name=ngsa-gateway

# check the version endpoint
# you may get a timeout error, if so, just retry

http ${Ngsa_App_Endpoint}/version

```

Check that the test certificates have been issued. You can check in the browser, or use curl. With the test certificates, it is expected that you get a privacy error.

```bash

export Ngsa_Https_App_Endpoint="https://${Ngsa_App_Endpoint}"

# Send a request to the https endpoint. You should see a certificate problem. This is expected with the staging certificates from Let's Encrypt.
http $Ngsa_Https_App_Endpoint/version

```

## Upgrade to Let's Encrypt production certs

After verifying that the test certs were issued, update the deployment to use the "letsencrypt-prod" ClusterIssuer to get valid certs from the Let's Encrypt production environment.

```bash

cd $REPO_ROOT/spikes/lets-encrypt

envsubst < prod-gateway.yaml | kubectl apply -n ngsa -f -

envsubst < prod-certificate.yaml |  kubectl apply -n istio-system -f -

# Send a request to the https endpoint. You should now get a successful response
http $Ngsa_Https_App_Endpoint/version

```
