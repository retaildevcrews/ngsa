### Build and Test the App
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS test

SHELL ["/bin/bash", "-c"]

# dotnet compiler options
ARG CONFIGURATION=
ARG TAG=false

ENV DEBIAN_FRONTEND=noninteractive
# Install the Azure CLI
#RUN mkdir -p /root/.azure \
#    && apt-get update \
#    && apt-get -y install --no-install-recommends apt-utils dialog lsb-release curl \
    #
    # Install the Azure CLI
#    && echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" > /etc/apt/sources.list.d/azure-cli.list \
#    && curl -sL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - 2>/dev/null \
#    && apt-get update \
#    && apt-get install -y azure-cli

ENV DEBIAN_FRONTEND=dialog

EXPOSE 4120

### copy the source and tests
COPY . /src

WORKDIR /src/app

# build the app
RUN dotnet publish -c Release -o /app

###########################################################


### Build the runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS release

### if port is changed, also update value in Constants.cs
EXPOSE 4120
WORKDIR /app

### create a user
### dotnet needs a home directory
RUN addgroup -S ngap && \
    adduser -S ngap -G ngap && \
    mkdir -p /home/ngap && \
    chown -R ngap:ngap /home/ngap

### run as ngap user
USER ngap

### copy the app
COPY --from=test /app .

ENTRYPOINT [ "dotnet",  "ngap.dll" ]
