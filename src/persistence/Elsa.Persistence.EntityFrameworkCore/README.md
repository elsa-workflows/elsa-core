# Generating migrations

We should generate migration sets per provider as described here: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli 

Example commands:

```bash
dotnet ef migrations add Update01 --context SqliteContext --output-dir Migrations/Sqlite
dotnet ef migrations add Update01 --context SqlServerContext --output-dir Migrations/SqlServer
dotnet ef migrations add Update01 --context PostgreSqlContext --output-dir Migrations/PostgreSql
dotnet ef migrations add Update01 --context MySqlContext --output-dir Migrations/MySql
```
 

# Creating and updating the Sqlite database

First, set the appropriate connection string.
To create the database, run the migrations.

Example command:

 ```bash
SET EF_CONNECTIONSTRING=Data Source=c:\data\elsa.db;Cache=Shared
dotnet ef database update --context SqliteContext

SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;User=sa;Password=Secret_password123!;
dotnet ef database update --context SqlServerContext

SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;Port=5432;User Id=postgres;Password=Secret_password123!
dotnet ef database update --context PostgreSqlContext

SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;uid=developer;pwd=Secret_password123!
dotnet ef database update --context MySqlContext
``` 