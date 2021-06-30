### Configure persistence in appsettings.json

EF Sqlite
```
"Elsa": {
"Features": [
    "Persistence:EntityFrameworkCore:Sqlite",
    ...
    ],
}
```

EF MySQL
```
"Elsa": {
"Features": [
    "Persistence:EntityFrameworkCore:MySql",
    ...
    ],
}
```

EF SQL Server
```
"Elsa": {
"Features": [
    "Persistence:EntityFrameworkCore:SqlServer",
    ...
    ],
}
```

EF Postgre
```
"Elsa": {
"Features": [
    "Persistence:EntityFrameworkCore:PostgreSql",
    ...
    ],
}
```

EF MongoDB
```
"Elsa": {
"Features": [
    "Persistence:MongoDb",
    ...
    ],
}
```

YesSql Sqlite
```
"Elsa": {
"Features": [
    "Persistence:YesSql:Sqlite",
    ...
    ],
}
```

YesSql MySQL
```
"Elsa": {
"Features": [
    "Persistence:YesSql:MySql",
    ...
    ],
}
```

YesSql SQL Server
```
"Elsa": {
"Features": [
    "Persistence:YesSql:SqlServer",
    ...
    ],
}
```

YesSql Postgre
```
"Elsa": {
"Features": [
    "Persistence:YesSql:PostgreSql",
    ...
    ],
}
```

### Configure connection strings for each persistence

```
  "ConnectionStrings": {
    ...
    "MySql": "Server=LAPTOP-B76STK67;Database=Elsa;Uid=myUsername;Pwd=myPassword;",
    "SqlServer": "Server=LAPTOP-B76STK67;Database=Elsa;Integrated Security=true;MultipleActiveResultSets=True;Max Pool Size=500;Connection Timeout=3600",
    "Sqlite": "Data Source=elsa.sqlite.db;Cache=Shared;",
    "MongoDb": "mongodb://localhost:27017/Elsa"
  },
```

### Connection string examples
MySql
https://www.connectionstrings.com/mysql/
SQL Server
https://www.connectionstrings.com/sql-server/
Postgres
https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING
MongoDb
https://docs.mongodb.com/manual/reference/connection-string/