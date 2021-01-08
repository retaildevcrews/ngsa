# setup nginx

In order to run nginx in a pod with https, we need to setup `letsencrypt` or use a `self-signed` cert (which will give a cert warning)

We mount these files as volumes in the nginx pod via `lb.yaml`

- volumes mounted
  - /etc/ngsa/certbot
    - let's encrypt (and self-signed) certs
  - /etc/ngsa/conf.d
    - nginx config
  - /etc/ngsa/www
    - nginx www root

## self-signed cert setup

Start by setting up the self signed cert

``` bash

###################
# change this value
export MY_DOMAIN=your_FQDN

# create directories for nginx volumes
sudo mkdir -p /etc/ngsa
sudo mkdir -p /etc/ngsa/www/certbot
sudo mkdir -p /etc/ngsa/conf.d
sudo mkdir -p /etc/ngsa/certbot/live/${MY_DOMAIN}

# take ownership (makes debugging / editing easier)
sudo chown -R ${USER}:${USER} /etc/ngsa

# get the certbot files
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > /etc/ngsa/certbot/options-ssl-nginx.conf
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > /etc/ngsa/certbot/ssl-dhparams.pem

# create a self-signed cert (nginx won't run without a cert)
openssl req -x509 -nodes -newkey rsa:1024 -days 365 \
-keyout /etc/ngsa/certbot/live/${MY_DOMAIN}/privkey.pem \
-out /etc/ngsa/certbot/live/${MY_DOMAIN}/fullchain.pem \
-subj '/CN=localhost'

# create an app config for nginx
sed -e "s/{MY_DOMAIN}/${MY_DOMAIN}/g" template.conf > /etc/ngsa/conf.d/ngsa.conf

# create the load balancer
k apply -f lb.yaml

# make sure everything is running
k get all

# check the endpoint
# redirect
curl http://${PIP}/version

# you will get a warning on the self-signed cert
curl https://${PIP}/version

# ignore self-signed cert warning
curl -k https://${PIP}/version
