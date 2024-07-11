FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/samples/aspnet/Elsa.Samples.ProtoActorRuntime/Elsa.Samples.ProtoActorRuntime.csproj", "src/samples/aspnet/Elsa.Samples.ProtoActorRuntime/"]
RUN dotnet restore "src/samples/aspnet/Elsa.Samples.ProtoActorRuntime/Elsa.Samples.ProtoActorRuntime.csproj"
COPY . .
WORKDIR "/src/src/samples/aspnet/Elsa.Samples.ProtoActorRuntime"
RUN dotnet build "Elsa.Samples.ProtoActorRuntime.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Samples.ProtoActorRuntime.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Samples.ProtoActorRuntime.dll"]
