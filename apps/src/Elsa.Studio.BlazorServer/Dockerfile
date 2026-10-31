# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG ElsaVersion=3.9.0
ARG ElsaStudioVersion=3.9.0
ARG ElsaExtensionsVersion=3.9.0
WORKDIR /src

# Copy project and config files
COPY Directory.Build.props ../../Directory.Build.props
COPY Directory.Packages.props ../../Directory.Packages.props
COPY NuGet.Config ../../NuGet.Config
COPY src/Elsa.Studio.BlazorServer/Elsa.Studio.BlazorServer.csproj ./
COPY src/Elsa.Server.Shared/Elsa.Server.Shared.csproj ../Elsa.Server.Shared/
COPY src/Shared/Shared.csproj ../Shared/

# Restore dependencies
RUN dotnet restore "Elsa.Studio.BlazorServer.csproj" \
    /p:ElsaVersion=${ElsaVersion} \
    /p:ElsaStudioVersion=${ElsaStudioVersion} \
    /p:ElsaExtensionsVersion=${ElsaExtensionsVersion}

# Copy the rest of the source code
COPY src/Elsa.Studio.BlazorServer/. ./
COPY src/Elsa.Server.Shared/. ../Elsa.Server.Shared/
COPY src/Shared/. ../Shared/

# Build the project
RUN dotnet build "Elsa.Studio.BlazorServer.csproj" -c Release -o /app/build --no-restore \
    /p:ElsaVersion=${ElsaVersion} \
    /p:ElsaStudioVersion=${ElsaStudioVersion} \
    /p:ElsaExtensionsVersion=${ElsaExtensionsVersion}

# Publish stage
FROM build AS publish
RUN dotnet publish "Elsa.Studio.BlazorServer.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore \
    /p:ElsaVersion=${ElsaVersion} \
    /p:ElsaStudioVersion=${ElsaStudioVersion} \
    /p:ElsaExtensionsVersion=${ElsaExtensionsVersion}

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
COPY --from=build /src/obj/project.assets.json /app/elsa-project.assets.json

ENTRYPOINT ["dotnet", "Elsa.Studio.BlazorServer.dll"]
