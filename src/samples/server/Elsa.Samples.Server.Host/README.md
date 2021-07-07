### Configure persistence for Elsa Core in the appsettings.json

In order to specify the persistence it's required to set enabled status and connection string name in Features node

EF Sqlite
```
"Elsa": {
  "Features": {
    "PersistenceEntityFrameworkCoreSqlite": {
    "Enabled": true,
    "ConnectionStringName": "Sqlite"
    },
    ...
```

EF MySQL
```
"Elsa": {
  "Features": {
    "PersistenceEntityFrameworkCoreMySql": {
    "Enabled": true,
    "ConnectionStringName": "MySql"
    },
    ...
```

EF SQL Server
```
"Elsa": {
  "Features": {
    "PersistenceEntityFrameworkCoreSqlServer": {
    "Enabled": true,
    "ConnectionStringName": "SqlServer"
    },
    ...  
```

EF Postgre
```
"Elsa": {
  "Features": {
    "PersistenceEntityFrameworkCorePostgreSql": {
    "Enabled": "true",
    "ConnectionStringName": "PostgreSql"
    },
    ...
```

EF MongoDB
```
"Elsa": {
  "Features": {
    "PersistenceMongoDb": {
    "Enabled": true,
    "ConnectionStringName": "MongoDb"
    },
    ...
```

YesSql Sqlite
```
"Elsa": {
  "Features": {
    "PersistenceYesSqlSqlite": {
    "Enabled": true,
    "ConnectionStringName": "Sqlite"
    },
    ...
```

YesSql MySQL
```
"Elsa": {
  "Features": {
    "PersistenceYesSqlMySql": {
    "Enabled": true,
    "ConnectionStringName": "MySql"
    },
    ...
```

YesSql SQL Server
```
"Elsa": {
  "Features": {
    "PersistenceYesSqlSqlServer": {
    "Enabled": true,
    "ConnectionStringName": "SqlServer"
    },
    ...
```

YesSql Postgre
```
"Elsa": {
  "Features": {
    "PersistenceYesSqlPostgreSql": {
    "Enabled": true,
    "ConnectionStringName": "PostgreSql"
    },
    ...
```

### Configure modular activity providers such as Webhooks
It is required to add modular activity provider and additional persistence feature for your modular activity provider.
Two examples below are given to set up Elsa Core and Webhooks persistence as follow.

EF Sqlite
```
"Elsa": {
  "Features": {
    "PersistenceEntityFrameworkCoreSqlite": {
      "Enabled": true,
      "ConnectionStringName": "Sqlite"
    },
    "Webhooks": true
    },
    "WebhooksPersistenceEntityFrameworkCoreSqlite": {
      "Enabled": true,
      "ConnectionStringName": "Sqlite"
    }
    ...
```

EF MongoDB
```
"Elsa": {
  "Features": {
    "PersistenceMongoDb": {
      "Enabled": true,
      "ConnectionStringName": "MongoDb"
    },
    "Webhooks": true
    },
    "WebhooksPersistenceMongoDb": {
      "Enabled": true,
      "ConnectionStringName": "MongoDb"
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