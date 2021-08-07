# This dockerfile requires that all NPM packages there installed and build. 

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim-amd64 AS build
WORKDIR /source

# restore packages
COPY ./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj ./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj
RUN dotnet restore "./src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/ElsaDashboard.Samples.AspNetCore.Monolith.csproj"

# copy required source code
COPY src/. ./src
COPY *.props ./
COPY ./Nuget.Config ./

# build and publish (UseAppHost=false created platform independent binaries)
WORKDIR /source/src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith
RUN dotnet build "ElsaDashboard.Samples.AspNetCore.Monolith.csproj" -c Release -o /app/build
RUN dotnet publish "ElsaDashboard.Samples.AspNetCore.Monolith.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net5.0

# move binaries into smaller base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 80/tcp
EXPOSE 443/tcp
ENTRYPOINT ["dotnet", "ElsaDashboard.Samples.AspNetCore.Monolith.dll"]
