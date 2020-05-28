FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS base

WORKDIR /app

COPY *.dll /app/
COPY *.json *.config /app/

EXPOSE 80
EXPOSE 443

# ===== Additional Debug configurations =====

# set additional environment variables
ARG BUILD_CONFIGURATION=Debug
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# install debugger
RUN apt update && \
    apt install procps unzip -y && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

# include symbol files
COPY *.pdb /app/

# ===========================================

ENTRYPOINT ["dotnet", "MyService.EntryPoint.dll"]