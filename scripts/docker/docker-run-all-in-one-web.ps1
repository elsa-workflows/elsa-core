$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$dockerfile = Join-Path $repositoryRoot 'docker/ElsaServerAndStudio.Dockerfile'

docker build -t elsa-all-in-one-web:local -f $dockerfile $repositoryRoot
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -e ASPNETCORE_HTTP_PORTS=8080 -p 24000:8080 elsa-all-in-one-web:local
exit $LASTEXITCODE
