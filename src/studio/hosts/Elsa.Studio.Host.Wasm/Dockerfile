FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Elsa.Studio.WebAssemblyApp/Elsa.Studio.WebAssemblyApp.csproj", "src/Elsa.Studio.WebAssemblyApp/"]
RUN dotnet restore "src/Elsa.Studio.WebAssemblyApp/Elsa.Studio.WebAssemblyApp.csproj"
COPY . .
WORKDIR "/src/src/Elsa.Studio.WebAssemblyApp"
RUN dotnet build "Elsa.Studio.WebAssemblyApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Studio.WebAssemblyApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Studio.WebAssemblyApp.dll"]
