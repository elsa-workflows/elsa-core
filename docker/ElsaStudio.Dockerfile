FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Node 22 for Studio ClientLib bundles (designer.css and *.entry.js).
COPY --from=node:22-bookworm /usr/local /usr/local

# copy sources.
COPY core/src/. ./core/src
COPY extensions/src/. ./extensions/src
COPY studio/src/. ./studio/src
COPY ./NuGet.Config ./
COPY *.props *.targets ./
COPY icon.png ./
COPY build/ConsolidatedPackageProof.targets ./build/
COPY scripts/integration-program/build_studio_clientlibs.sh ./scripts/integration-program/
COPY scripts/integration-program/consolidated-build/studio-clientlib-lockfiles ./scripts/integration-program/consolidated-build/studio-clientlib-lockfiles

# Studio-only Blazor Server host. Point this image at a separate Elsa API with
# Backend__Url (for example http://elsa-server:8080/elsa/api).
# ClientLib wwwroot files are generated, not checked in.
RUN node --version \
    && chmod +x scripts/integration-program/build_studio_clientlibs.sh \
    && scripts/integration-program/build_studio_clientlibs.sh .
RUN dotnet restore "./studio/src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/studio/src/hosts/Elsa.Studio.Host.Server
RUN dotnet build "Elsa.Studio.Host.Server.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Studio.Host.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net10.0

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
CMD ["dotnet", "Elsa.Studio.Host.Server.dll"]
