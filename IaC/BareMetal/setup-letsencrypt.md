# Generate letsencrypt certs for nginx

> These notes are rough

In order to run nginx in a pod with https, we need to setup `letsencrypt`

We mount these files as volumes in the nginx pod via `lb.yml`

``` bash

##############
# change these values
export MY_DOMAIN=ngsabm.cse.ms

# create a directory
# if you change this, you'll have to change in the commands below
#   AND the yml files

sudo mkdir -p /etc/ngsa
mkdir -p www/certbot
mkdir -p conf.d
mkdir -p certbot/live/${MY_DOMAIN}

# take ownership
sudo chown -R ${SUDO_USER}:${SUDO_USER} /etc/ngsa

# get the certbot files
cd /etc/ngsa/certbot
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > options-ssl-nginx.conf
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > ssl-dhparams.pem

# create a dummy cert so that nginx will run
cd live/${MY_DOMAIN}

openssl req -x509 -nodes -newkey rsa:1024 -days 1 \
-keyout './privkey.pem' \
-out './fullchain.pem' \
-subj '/CN=localhost'

cd /etc/ngsa

# create an app config for nginx
cat > conf.d/ngsa.conf <<EOF
server {
    listen 80;
    server_name ${MY_DOMAIN};
    root /var/www;

    location / {
        return 301 https://\$host\$request_uri;
    }

    # handle requests
    location / {
        try_files \$uri @server;
        sendfile off;
        proxy_no_cache 1;
        proxy_cache_bypass 1;
        add_header Last-Modified \$date_gmt;
        add_header Cache-Control 'no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0';
        if_modified_since off;
    }

    location @server {
        proxy_pass http://ngsa:4120;
        proxy_buffering off;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
    }
}

server {
    listen 443 ssl;
    server_name {MY_DOMAIN};

    location / {
        proxy_pass http://ngsa:4120
    }
}

include /etc/letsencrypt/options-ssl-nginx.conf;
ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

ssl_certificate /etc/letsencrypt/live/${MY_DOMAIN}/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/${MY_DOMAIN}/privkey.pem;

EOF

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
