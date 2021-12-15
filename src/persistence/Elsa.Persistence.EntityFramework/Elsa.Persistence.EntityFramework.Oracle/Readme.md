### Entity Framework Core Commands

**Generate Migrations**

```
dotnet ef migrations add Initial -- "{connection string}"

```

etc..

**Apply Migrations**

`dotnet ef database update -- "{connection string}"`

### Docker
To run Oracle as a docker container:

```
docker pull container-registry.oracle.com/database/express:latest
docker run -d --name oracle-express -e ORACLE_PWD=oracle1234 container-registry.oracle.com/database/express:latest
docker logs oracle-express
```

Connection string:

```
Data Source=localhost;User Id=SYS;Password=sys;DBA Privilege=SYSDBA;
```