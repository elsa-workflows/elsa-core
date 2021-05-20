docker build -t elsa-dashboard-and-server:local -f ./docker/Dockerfile-elsa-dashboard-and-server .
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -p 11000:80 elsa-dashboard-and-server:local