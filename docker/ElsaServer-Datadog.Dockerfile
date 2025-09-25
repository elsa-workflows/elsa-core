# Version: 1
# Description: Dockerfile for building and running Elsa Server with Datadog and OpenTelemetry auto-instrumentation

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /source

# Copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# Restore packages.
RUN dotnet restore "./src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj"

# Build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/bundles/Elsa.Server.Web
RUN dotnet build "Elsa.Server.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Server.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net9.0

# Move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install runtime dependencies, including CA certificates.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        libpython3.11 \
        python3.11 \
        python3.11-dev \
        python3-pip \
        unzip \
        wget \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set PYTHONNET_PYDLL environment variable
ENV PYTHONNET_PYDLL=/usr/lib/aarch64-linux-gnu/libpython3.11.so

# Set environment variables for OpenTelemetry Auto-Instrumentation
ENV OTEL_DOTNET_AUTO_HOME=/otel \
    OTEL_LOG_LEVEL="debug"

# Download and extract OpenTelemetry Auto-Instrumentation
ARG OTEL_VERSION=1.7.0
RUN mkdir -p /otel \
    && curl -L -o /otel/otel-dotnet-install.sh "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh" \
    && chmod +x /otel/otel-dotnet-install.sh \
    && /bin/bash /otel/otel-dotnet-install.sh \
    && chmod +x /otel/instrument.sh

EXPOSE 8080/tcp
EXPOSE 443/tcp

ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "Elsa.Server.Web.dll"]
