# Uncategorized Notes

## How do we "dump and recover quickly"?

- MTTF vs. MTTR (recovery)
  - few large failures vs. more smaller failures
  - optimize for MTTR

- How do we limit access so we can feel safe having the tools in the images?

- What does k8s give us out of the box?
  - [jofultz] When I was looking at the fluentbit problem I exported logs I wanted to look through:
k logs [pod] -c [container] > [pod abbr]-[container abbr].log
  - Then just delete the pod and let k8s recreate it.

## document best practice

### from code review

- having --version and --dry-run is a best practice for CLIs
  - semver approach
- should we make it a requirement across our crews?
- building a standard Ubuntu image with tools / utils
  - optionally removing packages
- where do we document?

### /healthz  /healthz/ietf

- these are best practices
- should we break out into a component
- all the app would have to do is implement an interface or override a method or add a handler
- /version handler is in Ngsa.Middleware
- are there other endpoints like Prometheus that should be required?

### git Windows / Linux

When you copy or rename a file on Windows that has the X attribute on Linux, you lose that attribute. If that happens to be your docker entry point, your docker image doesn't work #AskMeHowIKnow

I think if you use git mv or git cp, it will keep the attribute, but need to test to verify.

Where can we capture this? It's bit us several times (as recently as today)

## k8s-quickstart

- it's not clear that we're using codespaces - it's in the base readme, but probably worth calling out how to start it.
- show what kubeadm pulled
  - docker images
- simplify ssh (I thought using --generate-ssh-keys would "always" generate new keys and it doesn't)
- If you use the Azure CLI to create your VM with the az vm create command, you can optionally generate SSH public and private key files using the --generate-ssh-keys option. The key files are stored in the ~/.ssh directory unless specified otherwise with the --ssh-dest-key-path option. If an ssh key pair already exists and the --generate-ssh-keys option is used, a new key pair will not be generated but instead the existing key pair will be used.
- on first SSH you will get an IP address prompt - yes
- after git clone on the VM
  - cd k8s-quickstart/02-Bare-metal-setup
- show / explain everything that's running
  - docker ps
  - k get all --all-namespaces

- In 02-Bare-metal-setup/app/README.md, update the section around

```bash

# open your local browser
http://127.0.0.1:4120/version

# to take codespaces into account. User would have to curl from codespace or maybe another port forward from codespace to access the service from the browser.

```

- For this section

```bash

ssh -L 4120:127.0.0.1:4120 codespace@YourIPAddress

kubectl port-forward svc/ngsa 4120:4120

# codespace users were running into issue forwarding the same port. I believe it was fixed by using different ports.

```
