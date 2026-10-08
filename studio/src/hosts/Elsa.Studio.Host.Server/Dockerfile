FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Elsa.Studio.ServerApp/Elsa.Studio.ServerApp.csproj", "Elsa.Studio.ServerApp/"]
RUN dotnet restore "Elsa.Studio.ServerApp/Elsa.Studio.ServerApp.csproj"
COPY . .
WORKDIR "/src/Elsa.Studio.ServerApp"
RUN dotnet build "Elsa.Studio.ServerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Studio.ServerApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Studio.ServerApp.dll"]
