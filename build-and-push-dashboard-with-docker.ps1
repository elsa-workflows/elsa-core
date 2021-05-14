docker build -t elsa-dashboard:latest -t elsa-workflows/elsa-dashboard:latest -t ndakota81/elsa-dashboard -f ./docker/Dockerfile-elsa-dashboard .
docker push ndakota81/elsa-dashboard:latest