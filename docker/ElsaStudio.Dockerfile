FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# restore packages.
RUN dotnet restore "./src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/apps/Elsa.ModularServer.Web
RUN dotnet build "Elsa.ModularServer.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.ModularServer.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net10.0

# move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install CA certificates so HTTPS works out of the box.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8080/tcp
EXPOSE 443/tcp
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "Elsa.ModularServer.Web.dll"]
