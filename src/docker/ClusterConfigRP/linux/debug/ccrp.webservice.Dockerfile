FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
ARG BUILD_CONFIGURATION=Debug
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 80
EXPOSE 443

RUN apt update && \
    apt install procps unzip -y && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

WORKDIR /app

COPY *.dll /app/
COPY *.pdb /app/
COPY *.json *.config /app/
ENTRYPOINT ["dotnet", "ClusterConfigRP.WebService.dll"]