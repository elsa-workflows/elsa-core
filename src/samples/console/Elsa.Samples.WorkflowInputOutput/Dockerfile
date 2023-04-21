FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/samples/console/Elsa.Samples.WorkflowInput/Elsa.Samples.WorkflowInput.csproj", "src/samples/console/Elsa.Samples.WorkflowInput/"]
RUN dotnet restore "src/samples/console/Elsa.Samples.WorkflowInput/Elsa.Samples.WorkflowInput.csproj"
COPY . .
WORKDIR "/src/src/samples/console/Elsa.Samples.WorkflowInput"
RUN dotnet build "Elsa.Samples.WorkflowInput.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Samples.WorkflowInput.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Samples.WorkflowInput.dll"]
