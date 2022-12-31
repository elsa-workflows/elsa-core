docker build -t elsa-3:local -f ./docker/Dockerfile .
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -p 13000:80 elsa-3:local