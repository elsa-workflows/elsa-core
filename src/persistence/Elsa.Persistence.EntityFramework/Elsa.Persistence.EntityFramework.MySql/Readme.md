### Usage

```
public void ConfigureServices(IServiceCollection services)
{

    services.AddElsa(elsa =>
        elsa
            .AddWorkflow<MyWorkflow>()
            .UseEntityFrameworkPersistence(
                contextOptions =>
                {
                    contextOptions.UseMySql(
                        "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;");
                },
                autoRunMigrations: true)
    );
}
```

### Entity Framework Core Commands

These commands are used by the package developer.

**Generate Migrations**

```
dotnet ef migrations add Initial
dotnet ef migrations add Update1
dotnet ef migrations add Update2
```

etc..

Optionally, specify a connection string:

```
dotnet ef migrations add Initial -- "{connection string}"
dotnet ef migrations add Update1 -- "{connection string}"
dotnet ef migrations add Update2 -- "{connection string}"
```

**Apply Migrations**


This 

`dotnet ef database update -- "{connection string}"`

