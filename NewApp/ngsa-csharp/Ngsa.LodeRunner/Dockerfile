### build the app
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# Copy the source
COPY NewApp/ngsa-csharp /src

### Run the unit tests
#WORKDIR /src/tests
#RUN dotnet test

### Build the release app
WORKDIR /src/Ngsa.LodeRunner
RUN dotnet publish -c Release -o /app

    
###########################################################


### build the runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS runtime

### create a user
### dotnet needs a home directory
RUN addgroup -S ngsa && \
    adduser -S ngsa -G ngsa && \
    mkdir -p /home/ngsa && \
    chown -R ngsa:ngsa /home/ngsa

WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/TestFiles && \
    cp *.json TestFiles && \
    cp perfTargets.txt TestFiles && \
    chown -R ngsa:ngsa /app

WORKDIR /app/TestFiles

# run as the ngsa user
USER ngsa

ENTRYPOINT [ "dotnet",  "../Ngsa.LodeRunner.dll" ]
