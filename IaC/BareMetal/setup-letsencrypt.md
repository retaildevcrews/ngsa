# Generate letsencrypt certs for nginx

> These notes are rough

In order to run nginx in a pod with https, we need to setup `letsencrypt`

We mount these files as volumes in the nginx pod via `lb.yml`

``` bash

# create a directory
# if you change this, you'll have to change in the commands below
#   AND the yml files

sudo mkdir -p /etc/ngsa
sudo chown -R bartr:bartr /etc/ngsa
cd /etc/ngsa

mkdir -p conf.d

# change ngsabm.cse.ms to your domain
# TODO - make an env var for dev experience

mkdir -p certbot/live/ngsabm.cse.ms
mkdir -p www/certbot

# get the certbot files
cd  certbot
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > options-ssl-nginx.conf
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > ssl-dhparams.pem

# create a dummy cert so that nginx will run
cd live/ngsabm.cse.ms
openssl req -x509 -nodes -newkey rsa:1024 -days 1 \
-keyout './privkey.pem' \
-out './fullchain.pem' \
-subj '/CN=localhost'

cd /etc/ngsa

# create an app config for ngninx
# TODO - use cat EOF for dev experience
nano conf.d/app.conf

server {
    listen 80;
    server_name ngsabm.cse.ms;
    location / {
        return 301 https://$host$request_uri;
    }
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }
}

server {
    listen 443 ssl;
    server_name ngsabm.cse.ms;

    location / {
        proxy_pass http://ngsa:4120
    }
}

ssl_certificate /etc/letsencrypt/live/ngsabm.cse.ms/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/ngsabm.cse.ms/privkey.pem;

include /etc/letsencrypt/options-ssl-nginx.conf;
ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

# EOF

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
rm certbot/live/ngsabm.cse.ms/*.pem

# run certbot
docker run -it --rm \
-v /etc/ngsa/certbot:/etc/letsencrypt \
-v /etc/ngsa/www:/var/www \
--entrypoint sh certbot/certbot

# test cert creation
# once it's working, remove the --dry-run param
# you WILL get locked out of letsencrypt if you run too many times on the same sub-domain

# TODO - change email to env var
certbot certonly --webroot -w /var/www/certbot \
--email bartr@outlook.com \
-d ngsabm.cse.ms \
--rsa-key-size 4096 \
--agree-tos \
--force-renewal \
--dry-run

# exit the container

# delete the containers
docker rm -f nginx
docker rm -f ngsa

# take ownership
sudo chown -R bartr:bartr .

# you now have nginx configured with letsencrypt cert
# you can deploy lb.yaml and test

```
