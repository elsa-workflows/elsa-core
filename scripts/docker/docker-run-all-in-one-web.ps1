docker build -t elsa-all-in-one-web:local -f ./src/apps/all-in-one-web/Dockerfile .
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -p 24000:80 elsa-all-in-one-web:local