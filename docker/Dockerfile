FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim-amd64 AS build
WORKDIR /source

# restore packages
COPY ./src/samples/dashboard/ElsaDashboard.Samples.Monolith/ElsaDashboard.Samples.Monolith.csproj ./src/samples/dashboard/ElsaDashboard.Samples.Monolith/ElsaDashboard.Samples.Monolith.csproj
RUN dotnet restore "./src/samples/dashboard/ElsaDashboard.Samples.Monolith/ElsaDashboard.Samples.Monolith.csproj"

# copy required source code
COPY src/. ./src
COPY *.props ./
COPY ./Nuget.config ./

# build and publish (UseAppHost=false created platform independent binaries)
WORKDIR /source/src/samples/dashboard/ElsaDashboard.Samples.Monolith
RUN dotnet build "ElsaDashboard.Samples.Monolith.csproj" -c Release -o /app/build
RUN dotnet publish "ElsaDashboard.Samples.Monolith.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore


# move binaries into smaller base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 80/tcp
ENTRYPOINT ["dotnet", "ElsaDashboard.Samples.Monolith.dll"]