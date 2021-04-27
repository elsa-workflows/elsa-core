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
dotnet ef migrations add Initial -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
dotnet ef migrations add Update1 -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
dotnet ef migrations add Update2 -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
```

etc...

Optionally provide a server version explicitly if you don't have MySql running locally:

`dotnet ef migrations add Initial -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;" "8.0.22"`

**Apply Migrations**

`dotnet ef database update -- "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"`

