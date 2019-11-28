FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY *.dll /app/
COPY *.json *.config /app/
ENTRYPOINT ["dotnet", "ClusterConfigRP.WebService.dll"]