### Build and Test the App
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS test

SHELL ["/bin/bash", "-c"]

# dotnet compiler options
ARG TAG=false

EXPOSE 4120

### copy the source and tests
COPY NewApp/ngsa-csharp /src

WORKDIR /src/Ngsa.DataService

# build the app
RUN if [ "$TAG" == "true" ]; then dotnet publish -c Release -o /app --version-suffix ''; else dotnet publish -c Release -o /app; fi

WORKDIR /src/Ngsa.DataService.Tests
ENTRYPOINT [ "./runtests" ]

###########################################################


### Build the runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS release

### if port is changed, also update value in Constants.cs
EXPOSE 4122
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

ENTRYPOINT [ "dotnet",  "Ngsa.DataService.dll" ]
