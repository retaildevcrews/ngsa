# Canonical multipass setup

## Instant Ubuntu VMs

- A mini-cloud on your Mac or Windows workstation.

## More information

- Canonical [multipass](https://multipass.run/)
- [cloud-init](https://ubuntu.com/blog/using-cloud-init-with-multipass)
- [cloud-config](https://cloudinit.readthedocs.io/en/latest/topics/examples.html)

## Setup

```bash

# using Windows

# change this if you want
set MY_VM=k8s

# run the VM
# 2 cores; 2G memory; 10G disk; Ubuntu 18.04 LTS
multipass launch -c 2 -d 10G -m 2G --cloud-init k8s.yaml -n %MY_VM% 18.04

# ssh into the VM
multipass shell %MY_VM%

# run final setup within the Ubuntu VM
sudo ../setup.sh

# reload shell
exit

# ssh into the VM
multipass shell %MY_VM%

```

## Editing k8s.yaml (you shouldn't need to)

> multipass will timeout during VM creation, so don't ask it to do too much

The `write_file` commands require base 64 encoded content

on Mac / Linux

- `base64 src/daemon.json`
- `base64 src/k8s.sh`
- replace content in k8s.yaml
  - make sure to indent the content properly

on Windows

- `certutil -encode src\daemon.json daemon.b64`
  - remove first and last line from daemon.b64
  - make sure to indent the content correctly

> this will NOT work on .sh file due to CRLF
