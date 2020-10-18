### Build and Test the App
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS test

SHELL ["/bin/bash", "-c"]

# dotnet compiler options
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
COPY src /src

WORKDIR /src/app

# build the app
RUN if [ "$TAG" == "true" ]; then dotnet publish -c Release -o /app --version-suffix ''; else dotnet publish -c Release -o /app; fi

WORKDIR /src/tests
ENTRYPOINT [ "./runtests" ]

###########################################################


### Build the runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS release

### if port is changed, also update value in Constants.cs
EXPOSE 4120
WORKDIR /app

### create a user
### dotnet needs a home directory
RUN addgroup -S ngsa && \
    adduser -S ngsa -G ngsa && \
    mkdir -p /home/ngsa && \
    chown -R ngsa:ngsa /home/ngsa

### run as ngsa user
USER ngsa

### copy the app
COPY --from=test /app .

ENTRYPOINT [ "dotnet",  "ngsa.dll" ]
