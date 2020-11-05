### Build and Test the App
FROM mcr.microsoft.com/dotnet/core/sdk:3.1

SHELL ["/bin/bash", "-c"]

RUN apt-get update && \
    curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add - && \
    echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" > /etc/apt/sources.list.d/kubernetes.list && \
    apt-get update && \
    apt-get install -y kubectl kubeadm

### copy the source and tests
COPY src/log-agent /src

WORKDIR /src


# build the app
RUN mkdir -p /app && dotnet publish -o /app

WORKDIR /app
ENTRYPOINT [ "./logagent" ]
