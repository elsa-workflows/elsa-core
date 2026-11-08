FROM --platform=$BUILDPLATFORM node:22-bookworm AS node
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Node 22 for Studio ClientLib bundles (designer.css and *.entry.js).
COPY --from=node /usr/local /usr/local

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

# Combined Server+Studio Blazor Server host. Studio talks to the in-process API at
# Backend__Url (default http://127.0.0.1:8080/elsa/api). ClientLib wwwroot files are
# generated, not checked in. The image only builds them; the ClientLib tests run in CI.
RUN node --version \
    && chmod +x scripts/integration-program/build_studio_clientlibs.sh \
    && STUDIO_CLIENTLIB_SKIP_TESTS=1 scripts/integration-program/build_studio_clientlibs.sh .
RUN dotnet restore "./studio/src/hosts/Elsa.ServerAndStudio.Web/Elsa.ServerAndStudio.Web.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/studio/src/hosts/Elsa.ServerAndStudio.Web
RUN dotnet build "Elsa.ServerAndStudio.Web.csproj" -c Release -o /app/build -f net10.0
RUN dotnet publish "Elsa.ServerAndStudio.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net10.0

# move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install runtime dependencies, including CA certificates.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libpython3-dev \
        python3 \
        python3-dev \
        python3-pip \
    && update-ca-certificates \
    && ln -s "$(find /usr/lib -name 'libpython3*.so' -print -quit)" /usr/local/lib/libpython3.so \
    && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set PYTHONNET_PYDLL environment variable
ENV PYTHONNET_PYDLL=/usr/local/lib/libpython3.so

EXPOSE 8080/tcp
EXPOSE 443/tcp
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "Elsa.ServerAndStudio.Web.dll"]
