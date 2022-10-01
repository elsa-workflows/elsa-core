# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.245.2/containers/dotnet/.devcontainer/base.Dockerfile

# [Choice] .NET version: 6.0, 3.1, 6.0-bullseye, 3.1-bullseye, 6.0-focal, 3.1-focal
ARG VARIANT="6.0-bullseye-slim"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

# [Choice] Node.js version: none, lts/*, 18, 16, 14
ARG NODE_VERSION="none"
RUN if [ "${NODE_VERSION}" != "none" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>

# [Optional] Uncomment this line to install global node packages.
# RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g <your-package-here>" 2>&1