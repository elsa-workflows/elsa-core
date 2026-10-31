# Build from the repository root: docker build -f studio/react/Dockerfile .
FROM node:22-alpine AS assets
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

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG ELSA_VERSION=3.11.0
WORKDIR /source
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props NuGet.Config icon.png ./
COPY build/ConsolidatedPackageProof.targets build/
COPY studio/src/Directory.Build.props studio/src/Directory.Packages.props studio/src/
COPY studio/react/dotnet studio/react/dotnet/
COPY studio/react/studio.config.json studio/react/
COPY --from=assets /frontend/dist studio/react/dist/
COPY --from=assets /frontend/module-assets/ studio/react/dotnet/modules/
RUN --mount=type=secret,id=build-ca \
    if [ -f /run/secrets/build-ca ]; then export SSL_CERT_FILE=/run/secrets/build-ca; fi; \
    dotnet publish studio/react/dotnet/Elsa.ReactStudio.Server/Elsa.ReactStudio.Server.csproj -c Release -o /output -p:UseAppHost=false -p:Version=$ELSA_VERSION && \
    chmod -R a+rX /output

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ELSA_VERSION=3.11.0
LABEL org.opencontainers.image.title="Elsa React Studio Preview" \
      org.opencontainers.image.version="$ELSA_VERSION" \
      org.opencontainers.image.source="https://github.com/elsa-workflows/elsa-core"
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /output .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Elsa.ReactStudio.Server.dll"]
