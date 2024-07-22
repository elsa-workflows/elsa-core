# Version: 1
# Description: Dockerfile for building and running Elsa Server

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /source

# Determine the architecture and download the appropriate version of the tracer
RUN ARCH=$(if [ "$(uname -m)" = "x86_64" ]; then echo "amd64"; elif [ "$(uname -m)" = "aarch64" ]; then echo "arm64"; else echo "amd64"; fi) \
    && TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -Lo /tmp/datadog-dotnet-apm.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_${ARCH}.deb

# copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# restore packages.
RUN dotnet restore "./src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj"

# build and publish (UseAppHost=false creates platform independent binaries).
WORKDIR /source/src/apps/Elsa.Server.Web
RUN dotnet build "Elsa.Server.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.Server.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net8.0

# move binaries into smaller base image.
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

# Copy the tracer from build target
COPY --from=build /tmp/datadog-dotnet-apm.deb /tmp/datadog-dotnet-apm.deb
# Install the tracer
RUN mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && dpkg -i /tmp/datadog-dotnet-apm.deb \
    && rm /tmp/datadog-dotnet-apm.deb
 
# Enable the tracer
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json

EXPOSE 8080/tcp
EXPOSE 443/tcp
ENTRYPOINT ["dotnet", "Elsa.Server.Web.dll"]
