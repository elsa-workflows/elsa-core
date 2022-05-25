FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/apps/all-in-one-web/Elsa.AllInOne.Web.csproj", "apps/all-in-one-web/"]
COPY src .
COPY NuGet.Config .
RUN dotnet restore "apps/all-in-one-web/Elsa.AllInOne.Web.csproj"
WORKDIR "/src/apps/all-in-one-web"
RUN dotnet build "Elsa.AllInOne.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.AllInOne.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.AllInOne.Web.dll"]
