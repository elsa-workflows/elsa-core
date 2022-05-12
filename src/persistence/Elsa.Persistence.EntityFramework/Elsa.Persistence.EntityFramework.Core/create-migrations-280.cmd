REM MySql
cd ../Elsa.Persistence.EntityFramework.MySql
dotnet ef migrations add Update28 -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;" "8.0.22"

REM PostgreSql
cd ../Elsa.Persistence.EntityFramework.PostgreSql
dotnet ef migrations add Update28 -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;" "8.0.22"

REM Sqlite
cd ../Elsa.Persistence.EntityFramework.Sqlite
dotnet ef migrations add Update28

REM SqlServer
cd ../Elsa.Persistence.EntityFramework.SqlServer
dotnet ef migrations add Update28 -- "{connection string}"

REM Oracle
cd ../Elsa.Persistence.EntityFramework.Oracle
dotnet ef migrations add Update28 -- "{connection string}"