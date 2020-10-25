#!/bin/bash

# default user for multipass
export ME=ubuntu

# make some directories
mkdir -p /home/${ME}/bin
mkdir -p /home/${ME}/.local/bin
mkdir -p /home/${ME}/go/src
mkdir -p /home/${ME}/go/bin
mkdir -p /home/${ME}/go/pkg

# set the permissions on .ssh
chmod 700 /home/${ME}/.ssh
chmod 600 /home/${ME}/.ssh/*

# add user to groups
usermod -aG sudo ${ME}
usermod -aG admin ${ME}
usermod -aG docker ${ME}
sudo gpasswd -a ${ME} sudo

cd /home/${ME}

# clone this repo
git clone https://github.com/retaildevcrews/ngsa

# clone oh my bash and copy .bashrc
git clone --depth=1 https://github.com/ohmybash/oh-my-bash.git .oh-my-bash
cp .oh-my-bash/templates/bashrc.osh-template .bashrc

# add to .bashrc
echo "" >> .bashrc
echo 'export PATH="$PATH:$HOME/.dotnet/tools:$HOME/go/bin"' >> .bashrc

# optional but helpful
echo "" >> .bashrc
echo "export GO111MODULE=on" >> .bashrc
echo "alias k='kubectl'" >> .bashrc
echo "alias ipconfig='ip -4 a show eth0 | grep inet | sed \"s/inet//g\" | sed \"s/ //g\" | cut -d '/' -f 1'" >> .bashrc
echo 'export PIP=$(ipconfig)' >> .bashrc
echo 'echo $PIP' >> .bashrc

# update and install utilities
apt-get update
apt-get install -y apt-utils dialog apt-transport-https ca-certificates lsb-release curl git wget nano
apt-get install -y software-properties-common libssl-dev libffi-dev python-dev build-essential gnupg-agent
apt-get install -y curl git wget nano jq zip unzip httpie dnsutils

# add Docker repo
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add -
add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

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

# update sources
apt-get update

# install docker and k8s
apt-get install -y docker-ce docker-ce-cli containerd.io kubectl kubelet kubeadm kubernetes-cni

# install azure cli and dotnet
apt-get install -y azure-cli dotnet-sdk-3.1

# start docker service at boot
systemctl enable docker

# pull the images
kubeadm config images pull

# update / upgrade
apt-get update
apt-get autoremove -y
apt-get upgrade -y

# take ownership of home directory
chown -R  ${ME}:${ME} /home/${ME}

# display the IP address for convenience
ip -4 a show eth0 | grep inet | sed "s/inet//g" | sed "s/ //g" | cut -d '/' -f 1
