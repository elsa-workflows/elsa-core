docker build -t elsa-3:local -f ./docker/Dockerfile .
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -e HTTP_PORTS=8080 -p 13000:8080 elsa-3:local