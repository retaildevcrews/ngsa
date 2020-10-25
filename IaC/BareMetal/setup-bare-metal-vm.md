# k8s Setup (bare metal)

> Setup a k8s bare metal cluster

- VM Requirements
  - minimum 2 GB / 2 core for k8s controller
  - make sure to use `Ubuntu 18.04`
  - copy the setup script below and paste into the `User data` section when creating VMs

## Validation

From the VM

```bash

# make sure this file exists
cat /etc/docker/daemon.json

# this will tell you when the user data script is done
cat status

# make sure everything is up to date
sudo apt update
sudo apt upgrade -y

# reboot
sudo shutdown -r now

# ssh back into the VM

# return to the readme for k8s setup

```

## User Data

> Copy and paste the below into "user data" when you create a VM

```bash

#!/bin/bash

########################################
# replace with your ID
# set to IS_AZURE to false to create user
export ME=ngsa
export IS_AZURE=true
########################################

# this must be first or useradd will return error
if [[ "$IS_AZURE" != "true" ]]; then
  useradd -m -s /bin/bash ${ME}
fi

mkdir -p /home/${ME}/.ssh
mkdir -p /home/${ME}/bin
mkdir -p /home/${ME}/.local/bin
mkdir -p /home/${ME}/go/src
mkdir -p /home/${ME}/go/bin
mkdir -p /home/${ME}/go/pkg

echo "starting" > /home/${ME}/status

if [[ "$IS_AZURE" != "true" ]]; then
  cp /root/.ssh/authorized_keys  /home/${ME}/.ssh
fi

cp /usr/share/zoneinfo/America/Chicago /etc/localtime
w

groupadd docker

usermod -aG sudo ${ME}
usermod -aG admin ${ME}
usermod -aG docker ${ME}
gpasswd -a ${ME} sudo

echo "${ME} ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/90-cloud-init-users

cd /home/${ME}

chown -R  ${ME}:${ME} /home/${ME}

# oh my bash
git clone --depth=1 https://github.com/ohmybash/oh-my-bash.git .oh-my-bash
cp .oh-my-bash/templates/bashrc.osh-template .bashrc

# add to .bashrc
echo "" >> .bashrc
echo "export GO111MODULE=on" >> .bashrc
echo "alias k='kubectl'" >> .bashrc
echo "alias ipconfig='ip -4 a show eth0 | grep inet | sed \"s/inet//g\" | sed \"s/ //g\" | cut -d / -f 1'" >> .bashrc
echo 'export PIP=$(ipconfig)' >> .bashrc
echo 'export PATH="$PATH:$HOME/.dotnet/tools:$HOME/go/bin"' >> .bashrc

# create .profile
if [[ ! -f ".profile" ]]; then

cat <<'EOF' > .profile
# if running bash
if [ -n "$BASH_VERSION" ]; then
    # include .bashrc if it exists
    if [ -f ".bashrc" ]; then
        . .bashrc
    fi
fi

# set PATH so it includes user's private bin if it exists
if [ -d "bin" ] ; then
    PATH="$HOME/bin:$PATH"
fi

# set PATH so it includes user's private bin if it exists
if [ -d ".local/bin" ] ; then
    PATH="$HOME/.local/bin:$PATH"
fi
EOF

fi

chown -R  ${ME}:${ME} /home/${ME}

echo "updating" > /home/${ME}/status

apt-get update

# add Docker repo
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add -
add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

mkdir -p /etc/systemd/system/docker.service.d
mkdir -p /etc/docker

# Set up the Docker daemon to use systemd
cat <<'EOF' > /etc/docker/daemon.json
{
  "exec-opts": ["native.cgroupdriver=systemd"],
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m"
  },
  "storage-driver": "overlay2"
}
EOF

apt-get update

echo "install apt" > /home/${ME}/status
apt-get install -y apt-utils dialog apt-transport-https ca-certificates curl

# add dotnet repo
echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list

# add Azure CLI repo
curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /etc/apt/trusted.gpg.d/microsoft.asc.gpg
echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" > /etc/apt/sources.list.d/azure-cli.list

# add kubenetes repo
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add -
echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" > /etc/apt/sources.list.d/kubernetes.list

# add go repo
add-apt-repository ppa:longsleep/golang-backports -y

apt-get update

echo "install utils" > /home/${ME}/status
apt-get install -y curl git wget nano jq zip unzip httpie dnsutils

echo "install libs" > /home/${ME}/status
apt-get install -y software-properties-common libssl-dev libffi-dev python-dev build-essential lsb-release gnupg-agent

echo "install docker" > /home/${ME}/status
apt-get install -y docker-ce docker-ce-cli containerd.io kubectl kubelet kubeadm kubernetes-cni

echo "install az" > /home/${ME}/status
apt-get install -y azure-cli

echo "install dotnet" > /home/${ME}/status
apt-get install -y dotnet-sdk-3.1

# echo "install go" > /home/${ME}/status
# apt-get install -y golang-go

systemctl enable docker

# set the permissions on .ssh
chmod 700 /home/${ME}/.ssh
chmod 600 /home/${ME}/.ssh/*

# pull the images
kubeadm config images pull

echo "updating" > /home/${ME}/status
apt-get update
apt-get autoremove -y
apt-get upgrade -y

echo "done" > status

chown -R  ${ME}:${ME} /home/${ME}

```
