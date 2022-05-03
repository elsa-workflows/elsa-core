FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/samples/aspnet/Elsa.Samples.Web3/Elsa.Samples.Web3.csproj", "Elsa.Samples.Web3/"]
RUN dotnet restore "src/samples/aspnet/Elsa.Samples.Web3/Elsa.Samples.Web3.csproj"
COPY . .
WORKDIR "/src/Elsa.Samples.Web3"
RUN dotnet build "Elsa.Samples.Web3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Samples.Web3.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Samples.Web3.dll"]
