# This dockerfile requires that all NPM packages there installed and build. 

FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim-amd64 AS build
WORKDIR /source

# restore packages
COPY ./Nuget.Config ./
COPY ./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj ./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj
RUN dotnet restore "./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj"  --configfile ./Nuget.Config

# copy required source code
COPY src/. ./src
COPY *.props ./

# build and publish (UseAppHost=false created platform independent binaries)
WORKDIR /source/src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith
RUN dotnet build "ElsaDashboard.Samples.AspNetCore.Monolith.csproj" -c Release -o /app/build
RUN dotnet publish "ElsaDashboard.Samples.AspNetCore.Monolith.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net7.0

# move binaries into smaller base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 80/tcp
EXPOSE 443/tcp
ENTRYPOINT ["dotnet", "ElsaDashboard.Samples.AspNetCore.Monolith.dll"]
