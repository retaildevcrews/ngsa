#!/bin/bash

########################################
# replace with your ID
# set IS_AZURE to false to create user
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

cd $HOME
# add to su .bashrc
echo "" >> .bashrc
echo "alias k='kubectl'" >> .bashrc
echo "alias ipconfig='ip -4 a show eth0 | grep inet | sed \"s/inet//g\" | sed \"s/ //g\" | cut -d / -f 1'" >> .bashrc
echo "alias kall='kubectl get all --all-namespaces'" >> .bashrc
echo 'export PIP=$(ipconfig | tail -n 1)' >> .bashrc
. .bashrc

cd /home/${ME}

chown -R ${ME}:${ME} /home/${ME}

# oh my bash
git clone --depth=1 https://github.com/ohmybash/oh-my-bash.git .oh-my-bash
cp .oh-my-bash/templates/bashrc.osh-template .bashrc

# add to .bashrc
echo "" >> .bashrc
echo "export GO111MODULE=on" >> .bashrc
echo "alias k='kubectl'" >> .bashrc
echo "alias ipconfig='ip -4 a show eth0 | grep inet | sed \"s/inet//g\" | sed \"s/ //g\" | cut -d / -f 1'" >> .bashrc
echo "alias kall='kubectl get all --all-namespaces'" >> .bashrc
echo 'export PIP=$(ipconfig | tail -n 1)' >> .bashrc
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
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key --keyring /etc/apt/trusted.gpg.d/docker.gpg add -
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

echo "install base" > /home/${ME}/status
apt-get update
apt-get install -y apt-utils dialog apt-transport-https ca-certificates curl software-properties-common

# add dotnet repo
echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list

# add Azure CLI repo
curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /etc/apt/trusted.gpg.d/microsoft.asc.gpg
echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" > /etc/apt/sources.list.d/azure-cli.list

# add kubenetes repo
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add -
echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" > /etc/apt/sources.list.d/kubernetes.list

# add go repo
# add-apt-repository ppa:longsleep/golang-backports -y

apt-get update

echo "install utils" > /home/${ME}/status
apt-get install -y git wget nano jq zip unzip httpie dnsutils

echo "install libs" > /home/${ME}/status
apt-get install -y libssl-dev libffi-dev python-dev build-essential lsb-release gnupg-agent

echo "install k8s" > /home/${ME}/status
apt-get install -y containerd.io kubectl kubelet kubeadm kubernetes-cni

echo "install az" > /home/${ME}/status
#apt-get install -y azure-cli

echo "install dotnet" > /home/${ME}/status
#apt-get install -y dotnet-sdk-3.1

# echo "install go" > /home/${ME}/status
# apt-get install -y golang-go

# set the permissions on .ssh
chmod 700 /home/${ME}/.ssh
chmod 600 /home/${ME}/.ssh/*
chown -R  ${ME}:${ME} /home/${ME}

# containerd setup
# todo - validate
cat <<EOF >> /etc/sysctl.d/k8s.conf
net.bridge.bridge-nf-call-ip6tables = 1
net.bridge.bridge-nf-call-iptables = 1
EOF

# todo - is this a dupe?
# Setup required sysctl params, these persist across reboots.
cat <<EOF >> /etc/sysctl.d/99-kubernetes-cri.conf
net.bridge.bridge-nf-call-iptables  = 1
net.ipv4.ip_forward                 = 1
net.bridge.bridge-nf-call-ip6tables = 1
EOF

cat <<EOF >> /etc/modules-load.d/containerd.conf
overlay
br_netfilter
EOF

# Apply sysctl params
sysctl --system

# apply network changes
modprobe overlay
modprobe br_netfilter

# Configure containerd
mkdir -p /etc/containerd
containerd config default > /etc/containerd/config.toml

# Restart containerd
systemctl restart containerd

echo "kubeadm" > status

# pull the images
kubeadm config images pull

# init k8s
export PIP=$(ip -4 a show eth0 | grep inet | sed "s/inet//g" | sed "s/ //g" | cut -d '/' -f 1 | tail -n 1)
kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address $PIP

echo "k8s setup" > status

# setup your config file
mkdir -p $HOME/.kube
cp -i /etc/kubernetes/admin.conf $HOME/.kube/config

mkdir -p /home/${ME}/.kube
cp -i /etc/kubernetes/admin.conf /home/${ME}/.kube/config
chown -R ${ME}:${ME} /home/${ME}/.kube

# add flannel network overlay
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml --namespace=kube-system

# optional - add the taint to schedule normal pods on the control plane
# this let you run a "one node" cluster for `development`
kubectl taint nodes --all node-role.kubernetes.io/master-

# patch kube-proxy for metal LB
kubectl get configmap kube-proxy -n kube-system -o yaml | \
sed -e "s/strictARP: false/strictARP: true/" | \
sed -e 's/mode: ""/mode: "ipvs"/' | \
kubectl apply -f - -n kube-system

## Install metal LB
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.4/manifests/namespace.yaml
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.9.4/manifests/metallb.yaml
kubectl create secret generic -n metallb-system memberlist --from-literal=secretkey="$(openssl rand -base64 128)"

# create metal LB config map
curl https://raw.githubusercontent.com/retaildevcrews/ngsa/master/IaC/BareMetal/metalLB.yaml -o metalLB.yaml
sed -e "s/{PIP}/${PIP}/g" metalLB.yaml | kubectl apply -f -

echo "install docker" > /home/${ME}/status
apt-get install -y docker-ce docker-ce-cli
systemctl enable docker

chown -R ${ME}:${ME} /home/${ME}

echo "updating" > /home/${ME}/status
apt-get update
apt-get autoremove -y
apt-get upgrade -y
apt-mark hold kubelet kubeadm kubectl

echo "done" > /home/${ME}/status
