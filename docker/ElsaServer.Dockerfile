FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS build
WORKDIR /source

# copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# restore packages.
RUN dotnet restore "./src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/apps/Elsa.Server.Web
RUN dotnet build "Elsa.Server.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Server.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net10.0

# move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install runtime dependencies, including CA certificates.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libpython3.11 \
        python3.11 \
        python3.11-dev \
        python3-pip \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set PYTHONNET_PYDLL environment variable
ENV PYTHONNET_PYDLL=/usr/lib/aarch64-linux-gnu/libpython3.11.so

EXPOSE 8080/tcp
EXPOSE 443/tcp
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "Elsa.Server.Web.dll"]
