FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Server/RiftServer.csproj Server/
COPY Assets/Arena/Core/ Assets/Arena/Core/
RUN dotnet restore Server/RiftServer.csproj
COPY Server/ Server/
RUN dotnet publish Server/RiftServer.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "RiftServer.dll"]
