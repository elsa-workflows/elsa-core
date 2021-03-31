### Build and run localy

Clone the repository and open a command line in the project root folder. Then you can run the two docker commands below to build and start the container. Alternatively you can use the provided docker-compose file in the docker folder that does it for you via `docker-compuse up -d`. The docker-compose file will also include a fake smtp server to test email delivery.

```
$ docker build -t elsadashboard:local -f ./docker/Dockerfile .
$ docker run -t -i -e ELSA__HTTP__BASEURL='http://localhost' -e DASHBOARD__ELSASERVERURL='http://localhost:6868' -e ASPNETCORE_ENVIRONMENT='Development' -p 6868:80 elsadashboard:latest
```

The dashboard will be available on port `6868` and if you used the docker-compose file to start the application you will find a fake SMTP server on port `2525`.