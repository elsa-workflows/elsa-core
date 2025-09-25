FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /source

# copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# restore packages.
RUN dotnet restore "./src/apps/ElsaStudioWebAssembly/ElsaStudioWebAssembly.csproj"
RUN dotnet restore "./src/apps/Elsa.Studio.Web/Elsa.Studio.Web.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/apps/Elsa.Studio.Web
RUN dotnet build "Elsa.Studio.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Studio.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net9.0

# move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
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
CMD ["dotnet", "Elsa.Studio.Web.dll"]
