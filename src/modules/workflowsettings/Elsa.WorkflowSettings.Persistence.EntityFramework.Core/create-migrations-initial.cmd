REM MySql
cd ../Elsa.WorkflowSettings.Persistence.EntityFramework.MySql
dotnet ef migrations add Initial -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;" "8.0.22"

REM PostgreSql
cd ../Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql
dotnet ef migrations add Initial

REM Sqlite
cd ../Elsa.WorkflowSettings.Persistence.EntityFramework.Sqlite
dotnet ef migrations add Initial

REM SqlServer
cd ../Elsa.WorkflowSettings.Persistence.EntityFramework.SqlServer
dotnet ef migrations add Initial -- "{connection string}"