FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY *.dll /app/

ENTRYPOINT ["dotnet", "MyService.EntryPoint.dll"]