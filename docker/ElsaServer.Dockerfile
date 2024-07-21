FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /source

# copy sources.
COPY src/. ./src
COPY ./NuGet.Config ./
COPY *.props ./

# restore packages.
RUN dotnet restore "./src/bundles/Elsa.Server.Web/Elsa.Server.Web.csproj"

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
ENV PYTHONNET_PYDLL /usr/lib/aarch64-linux-gnu/libpython3.11.so

EXPOSE 80/tcp
EXPOSE 443/tcp
ENTRYPOINT ["dotnet", "Elsa.Server.Web.dll"]
