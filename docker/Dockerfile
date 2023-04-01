FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /source

# install node
ENV NODE_VERSION=19.0.0
RUN apt install -y curl
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.2/install.sh | bash
ENV NVM_DIR=/root/.nvm
RUN . "$NVM_DIR/nvm.sh" && nvm install ${NODE_VERSION}
RUN . "$NVM_DIR/nvm.sh" && nvm use v${NODE_VERSION}
RUN . "$NVM_DIR/nvm.sh" && nvm alias default v${NODE_VERSION}
ENV PATH="/root/.nvm/versions/node/v${NODE_VERSION}/bin/:${PATH}"
RUN node --version
RUN npm --version

# restore packages
COPY src/. ./src
COPY *.props ./
RUN dotnet restore "./src/bundles/Elsa.AllInOne.Web/Elsa.AllInOne.Web.csproj"

# build Elsa.Workflows.Designer npm package
WORKDIR /source/src/modules/Elsa.Workflows.Designer
RUN npm install
RUN npm run build

# build and publish (UseAppHost=false creates platform independent binaries)
WORKDIR /source/src/bundles/Elsa.AllInOne.Web
RUN dotnet build "Elsa.AllInOne.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Elsa.AllInOne.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore -f net7.0

# move binaries into smaller base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 80/tcp
EXPOSE 443/tcp
ENTRYPOINT ["dotnet", "Elsa.AllInOne.Web.dll"]
