FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/samples/aspnet/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps.csproj", "src/samples/aspnet/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps/"]
RUN dotnet restore "src/samples/aspnet/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps.csproj"
COPY . .
WORKDIR "/src/src/samples/aspnet/Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps"
RUN dotnet build "Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Samples.AspNet.ProtoActorRuntime.AzureContainerApps.dll"]
