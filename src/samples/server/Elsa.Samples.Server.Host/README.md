### Configure persistence for Elsa Core in the appsettings.json

It is required to specify the persistence, it's enabled status and connection string name in Features

EF Sqlite
```
"Elsa": {
  "Features": {
    "Persistence:EntityFrameworkCore:Sqlite": {
    "Enabled": "true",
    "ConnectionStringName": "Sqlite"
    },
    ...
```

EF MySQL
```
"Elsa": {
  "Features": {
    "Persistence:EntityFrameworkCore:MySql": {
    "Enabled": "true",
    "ConnectionStringName": "MySql"
    },
    ...
```

EF SQL Server
```
"Elsa": {
  "Features": {
    "Persistence:EntityFrameworkCore:SqlServer": {
    "Enabled": "true",
    "ConnectionStringName": "SqlServer"
    },
    ...  
```

EF Postgre
```
"Elsa": {
  "Features": {
    "Persistence:EntityFrameworkCore:PostgreSql": {
    "Enabled": "true",
    "ConnectionStringName": "PostgreSql"
    },
    ...
```

EF MongoDB
```
"Elsa": {
  "Features": {
    "Persistence:MongoDb": {
    "Enabled": "true",
    "ConnectionStringName": "MongoDb"
    },
    ...
```

YesSql Sqlite
```
"Elsa": {
  "Features": {
    "Persistence:YesSql:Sqlite": {
    "Enabled": "true",
    "ConnectionStringName": "Sqlite"
    },
    ...
```

YesSql MySQL
```
"Elsa": {
  "Features": {
    "Persistence:YesSql:MySql": {
    "Enabled": "true",
    "ConnectionStringName": "MySql"
    },
    ...
```

YesSql SQL Server
```
"Elsa": {
  "Features": {
    "Persistence:YesSql:SqlServer": {
    "Enabled": "true",
    "ConnectionStringName": "SqlServer"
    },
    ...
```

YesSql Postgre
```
"Elsa": {
  "Features": {
    "Persistence:YesSql:PostgreSql": {
    "Enabled": "true",
    "ConnectionStringName": "PostgreSql"
    },
    ...
```

### Configure modular activity providers such as Webhooks
It is required to add modular activity provider and additional persistence feature for your modular activity provider.
The persistence providers should match for Elsa Core and modular activity providers.
Two examples below are given to set up Elsa Core and Webhooks persistence as follow.

EF Sqlite
```
"Elsa": {
  "Features": {
    "Persistence:EntityFrameworkCore:Sqlite": {
    "Enabled": "true",
    "ConnectionStringName": "Sqlite"
    },
    "Webhooks": {
      "Enabled": "true"
    },
    "Webhooks:Persistence:EntityFrameworkCore:Sqlite": {
      "Enabled": "true",
      "ConnectionStringName": "Sqlite"
    }
    ...
```

EF MongoDB
```
"Elsa": {
  "Features": {
    "Persistence:MongoDb": {
    "Enabled": "true",
    "ConnectionStringName": "Sqlite"
    },
    "Webhooks": {
      "Enabled": "true"
    },
    "Webhooks:Persistence:MongoDb": {
      "Enabled": "true",
      "ConnectionStringName": "Sqlite"
    }
    ...
```

### Configure connection strings for each persistence

```
  "ConnectionStrings": {
    ...
    "MySql": "Server=LAPTOP-B76STK67;Database=Elsa;Uid=myUsername;Pwd=myPassword;",
    "SqlServer": "Server=LAPTOP-B76STK67;Database=Elsa;Integrated Security=true;MultipleActiveResultSets=True;Max Pool Size=500;Connection Timeout=3600",
    "Sqlite": "Data Source=elsa.sqlite.db;Cache=Shared;",
    "MongoDb": "mongodb://localhost:27017/Elsa",
    "PostgreSql": "Server=127.0.0.1;Port=5433;Database=elsa;User Id=postgres;Password=Password12!;"
  },
```

### Connection strings
MySql
https://www.connectionstrings.com/mysql/
SQL Server
https://www.connectionstrings.com/sql-server/
Postgres
https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING
MongoDb
https://docs.mongodb.com/manual/reference/connection-string/