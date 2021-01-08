# Generate lets encrypt certs for nginx

## Note on Rate Limits

>[Let's Encrypt](https://letsencrypt.org/) is used to issue TLS certificates.  Let's Encrypt has [rate limit policies](https://letsencrypt.org/docs/rate-limits/) that could be triggered if you run multiple deployments in sequence.  Please take note and be careful not to exceed their rate thresholds.

## Let's Encrypt Setup

In order to run nginx in a pod with https, we need to setup `letsencrypt` or use a `self-signed` cert (which will give a cert warning)

We mount these files as volumes in the nginx pod via `lb.yaml`

- volumes mounted
  - /etc/ngsa/certbot
    - let's encrypt (and self-signed) certs
  - /etc/ngsa/conf.d
    - nginx config
  - /etc/ngsa/www
    - nginx www root

```bash

# setup using docker

# create a network
docker network create ngsa

# run ngsa
docker run -d --name ngsa -p 4120:4120 --network ngsa retaildevcrew/ngsa:beta --in-memory

# check the logs
docker logs ngsa

# run nginx with the self-signed certs
docker run -d --name nginx -p 80:80 -p 443:443 --network ngsa \
-v /etc/ngsa/conf.d:/etc/nginx/conf.d \
-v /etc/ngsa/certbot:/etc/letsencrypt \
-v /etc/ngsa/www:/var/www \
nginx:1.15-alpine

# check the server
http localhost

# check the logs
docker logs nginx

# remove the self-signed certs
rm certbot/live/${MY_DOMAIN}/*.pem

# run certbot docker image
# make sure to mount the same volume as nginx uses
docker run -it --rm \
-v /etc/ngsa/certbot:/etc/letsencrypt \
-v /etc/ngsa/www:/var/www \
--entrypoint sh certbot/certbot

# test cert creation

##############################
# Change to your email address
# and domain
certbot certonly --webroot -w /var/www/certbot \
--email your_email@outlook.com \
-d your.fq.dn \
--rsa-key-size 4096 \
--agree-tos \
--force-renewal \
--dry-run

# once it's working, remove the --dry-run param
# you WILL get locked out of letsencrypt if you run too many times on the same sub-domain
##############################

# exit the container
exit

# docker clean up
docker rm -f nginx
docker rm -f ngsa
docker delete network ngsa

# take ownership
sudo chown -R ${USER}:${USER} .

# you now have nginx configured with letsencrypt cert
# you can deploy lb.yaml and test

```
