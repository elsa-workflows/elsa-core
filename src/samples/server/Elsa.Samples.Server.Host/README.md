### Configure persistence for Elsa Core in the appsettings.json

A simple way to enable a feature without persistence:

```
"Elsa": {
  "Features": {
    "Console": true
    ...
```

In order to specify persistence for a feature it's required to set enabled status, framework (has to be an empty space for MongoDb) and connection string identifier:

EF Sqlite
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "EntityFrameworkCore",
      "ConnectionStringIdentifier": "Sqlite"
    },
    ...
```

EF MySQL
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "EntityFrameworkCore",
      "ConnectionStringIdentifier": "MySql"
    },
    ...
```

EF SQL Server
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "EntityFrameworkCore",
      "ConnectionStringIdentifier": "SqlServer"
    },
    ...  
```

EF Postgre
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": "true",
      "Framework": "EntityFrameworkCore",
      "ConnectionStringIdentifier": "PostgreSql"
    },
    ...
```

MongoDB
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "",
      "ConnectionStringIdentifier": "MongoDb"
    },
    ...
```

YesSql Sqlite
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "YesSql",
      "ConnectionStringIdentifier": "Sqlite"
    },
    ...
```

YesSql MySQL
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "YesSql",
      "ConnectionStringIdentifier": "MySql"
    },
    ...
```

YesSql SQL Server
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "YesSql",
      "ConnectionStringIdentifier": "SqlServer"
    },
    ...
```

YesSql Postgre
```
"Elsa": {
  "Features": {
    "DefaultPersistence": {
      "Enabled": true,
      "Framework": "YesSql",
      "ConnectionStringIdentifier": "PostgreSql"
    },
    ...
```

### Workflow Settings is an optional feature and is controlled by IWorkflowSettingsProvider that currently has the database and configuration providers.
### The database provider uses one of available database persistence while the configuration one uses Environment Variables using IConfiguration
### The Environment Variable for the configuration provider can be set in the format as follows:

Name: "<WorkflowBlueprintId>:disabled"
Value: <boolean>

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