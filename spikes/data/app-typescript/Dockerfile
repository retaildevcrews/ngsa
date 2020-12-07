# ---- Base Node ----
FROM node:lts AS base

WORKDIR /app
COPY . .

RUN npm set progress=false && npm config set depth 0
RUN npm install --production
RUN cp -R node_modules prod_node_modules
RUN npm install
RUN npm run lint && npm run build && npm test
 
# ---- Release ----
FROM node:lts-alpine AS release

EXPOSE 4120
WORKDIR /app

### run as ngsa user
RUN adduser -S ngsa
USER ngsa

COPY --from=base /app/package.json .
COPY --from=base /app/prod_node_modules ./node_modules
COPY --from=base /app/dist ./dist
COPY --from=base /app/swagger/ngsa.json ./swagger/ngsa.json

ENTRYPOINT ["/usr/local/bin/npm", "start"]
