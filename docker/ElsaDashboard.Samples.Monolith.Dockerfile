FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy everything else and build app
COPY src/. ./src
COPY *.props ./
COPY Nuget.config ./
WORKDIR /source/src/samples/dashboard/ElsaDashboard.Samples.Monolith
RUN dotnet restore
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "ElsaDashboard.Samples.Monolith.dll"]