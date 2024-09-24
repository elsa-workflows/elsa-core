# Version: 1
# Description: Dockerfile for building and running Elsa Server

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /source

# Copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# Restore packages.
RUN dotnet restore "./src/bundles/Elsa.Server.Web/Elsa.Server.Web.csproj"

# Build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/bundles/Elsa.Server.Web
RUN dotnet build "Elsa.Server.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Server.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net8.0

# Move binaries into smaller base image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install Python 3.11
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3.11 \
    python3.11-dev \
    libpython3.11 \
    python3-pip && \
    rm -rf /var/lib/apt/lists/*

# Set PYTHONNET_PYDLL environment variable
ENV PYTHONNET_PYDLL=/usr/lib/aarch64-linux-gnu/libpython3.11.so

# Install dependencies
RUN apt-get update && apt-get install -y wget unzip curl

# Set environment variables for OpenTelemetry Auto-Instrumentation
ENV OTEL_DOTNET_AUTO_HOME=/otel
ENV OTEL_LOG_LEVEL="debug"

# Download and extract OpenTelemetry Auto-Instrumentation
ARG OTEL_VERSION=1.7.0
RUN mkdir /otel
RUN curl -L -o /otel/otel-dotnet-install.sh https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh
RUN chmod +x /otel/otel-dotnet-install.sh
RUN /bin/bash /otel/otel-dotnet-install.sh

# Provide necessary permissions for the script to execute
RUN chmod +x /otel/instrument.sh

EXPOSE 8080/tcp
EXPOSE 443/tcp

# Instrument the application and start it
ENTRYPOINT ["/bin/bash", "-c", "source /otel/instrument.sh && dotnet Elsa.Server.Web.dll"]