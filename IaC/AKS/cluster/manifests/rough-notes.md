# Rough notes for dns and ssl work

TODO: using this to keep some notes and use commands. will need to cleanup and move docs to appropriate places.

```bash

# deploy certmanager
kubectl apply -f https://github.com/jetstack/cert-manager/releases/download/v1.0.3/cert-manager.yaml

# deploy clusterissuers for letsencrypt. One for letsencrypt staging, another for letsencrypt production.
kubectl apply -f clusterissuer.yaml

# deploy certificate for domain to the same namespace where istio-ingressgateway is located
kubectl apply -f spikes/istio-multicluster/samples/helloworld/certificate.yaml -n istio-system

# use this to view the resources that certmanager will create in order to validate the http01 challenge
watch -n 1 kubectl get virtualservice,challenge,pods,ingress,orders --all-namespaces

# one way to view the transient resources that are created is to force the challenge to fail.
# this can be done by updating port 80 on the gateway to do an https redirect.
# there is probably a better way to view these resources. ex: the ingress resource that is created.

# from testing, noticed some resources are created then destroyed
- ingress with a route for the challenge. something like /.well-know/somthing/CHALLENGE_TOKEN
- acme pod for responding to the http challenge

# other certmanager resources are also created, challengerequest, order, challenge, etc. more info about the different resources here https://cert-manager.io/docs/concepts/

```
