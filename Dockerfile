FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY . ./

WORKDIR /app/src/dashboard/Elsa.Dashboard/Theme/argon-dashboard

RUN apt-get update -yq \
    && apt-get install build-essential -y \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash \
    && apt-get install nodejs -yq

RUN npm install
RUN npm install --global gulp-cli
RUN npm rebuild node-sass --force
RUN gulp build

WORKDIR /app
RUN dotnet publish Elsa.sln -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Elsa.Dashboard.Web.dll"]