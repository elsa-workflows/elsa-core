docker build -t elsa-server:latest -f ./docker/ElsaServer.Dockerfile .
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -e HTTP_PORTS=8080 -p 13000:8080 elsa-server:local