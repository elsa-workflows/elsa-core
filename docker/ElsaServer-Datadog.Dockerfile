FROM node:22-alpine AS react-assets
WORKDIR /frontend
RUN --mount=type=secret,id=build-ca \
    if [ -f /run/secrets/build-ca ]; then export NODE_EXTRA_CA_CERTS=/run/secrets/build-ca; fi; \
    npm install --global pnpm@10.33.0
COPY studio/react/package.json studio/react/pnpm-lock.yaml studio/react/pnpm-workspace.yaml ./
COPY studio/react/patches ./patches/
COPY studio/react/packages ./packages/
COPY studio/react/dotnet/modules ./dotnet/modules/
RUN --mount=type=secret,id=build-ca \
    if [ -f /run/secrets/build-ca ]; then export NODE_EXTRA_CA_CERTS=/run/secrets/build-ca; fi; \
    pnpm install --frozen-lockfile
COPY studio/react .
RUN --mount=type=secret,id=build-ca \
    if [ -f /run/secrets/build-ca ]; then export NODE_EXTRA_CA_CERTS=/run/secrets/build-ca; fi; \
    pnpm build


# Version: 1
# Description: Dockerfile for building and running Elsa Server with Datadog and OpenTelemetry auto-instrumentation

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
# One source snapshot avoids duplicating the SDK filesystem for every product.
# The root .dockerignore excludes local configuration, credentials and build outputs.
COPY . .
COPY --from=react-assets /frontend/dist ./studio/react/dist
COPY --from=react-assets /frontend/module-assets/ ./studio/react/dotnet/modules/
RUN --mount=type=secret,id=build-ca --mount=type=cache,target=/root/.nuget/packages \
    if [ -f /run/secrets/build-ca ]; then export SSL_CERT_FILE=/run/secrets/build-ca; fi; \
    dotnet publish core/src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj -c Release \
      -o /app/publish -p:UseAppHost=false -f net10.0

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/publish ./

# Install runtime dependencies, including CA certificates.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        libpython3-dev \
        python3 \
        python3-dev \
        python3-pip \
        unzip \
        wget \
    && update-ca-certificates \
    && ln -s "$(find /usr/lib -name 'libpython3*.so' -print -quit)" /usr/local/lib/libpython3.so \
    && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set PYTHONNET_PYDLL environment variable
ENV PYTHONNET_PYDLL=/usr/local/lib/libpython3.so

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
