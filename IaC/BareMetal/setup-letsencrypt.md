# Generate certs for nginx

> These notes are rough

In order to run nginx in a pod with https, we need to setup `letsencrypt` or use a `self-signed` cert (which will give a cert warning)

We mount these files as volumes in the nginx pod via `lb.yml`

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
k apply -f lb.yml

# make sure everything is running
k get all

# check the endpoint
# redirect
curl http://${PIP}/version

# you will get a warning on the self-signed cert
curl https://${PIP}/version

# ignore self-signed cert warning
curl -k https://${PIP}/version


###############
# let's encrypt instructions
# still need some work


# create a network
docker network create ngsa

# run ngsa
docker run -d --name ngsa -p 4120:4120 --network ngsa retaildevcrew/ngsa:beta --in-memory

# check the logs
docker logs ngsa

# run nginx
docker run -d --name nginx -p 80:80 -p 443:443 --network ngsa \
-v /etc/ngsa/conf.d:/etc/nginx/conf.d \
-v /etc/ngsa/certbot:/etc/letsencrypt \
-v /etc/ngsa/www:/var/www \
nginx:1.15-alpine

# check the server
http localhost

# check the logs
docker logs nginx

# remove the dummy certs
rm certbot/live/${MY_DOMAIN}/*.pem

# run certbot
docker run -it --rm \
-v /etc/ngsa/certbot:/etc/letsencrypt \
-v /etc/ngsa/www:/var/www \
--entrypoint sh certbot/certbot

# test cert creation

##############################
# Change to your email address
# and domain
certbot certonly --webroot -w /var/www/certbot \
--email bartr@outlook.com \
-d ngsabm.cse.ms \
--rsa-key-size 4096 \
--agree-tos \
--force-renewal \
--dry-run

# once it's working, remove the --dry-run param
# you WILL get locked out of letsencrypt if you run too many times on the same sub-domain

##############################
# Change to your email address
# and domain
certbot certonly --webroot -w /var/www/certbot \
--email bartr@outlook.com \
-d ngsabm.cse.ms \
--rsa-key-size 4096 \
--agree-tos \
--force-renewal

# exit the container
exit

# docker clean up
docker rm -f nginx
docker rm -f ngsa
docker delete network ngsa

# take ownership
sudo chown -R ${SUDO_USER}:${SUDO_USER} .

# you now have nginx configured with letsencrypt cert
# you can deploy lb.yml and test

```
