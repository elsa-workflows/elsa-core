# Fix for PersistenceFeatureBase SQLite Dependency Issue

## Problem Description

When creating custom persistence features that inherit from `PersistenceFeatureBase`, there was a conflict between database providers. The base class was automatically registering SQLite as a fallback database provider, which conflicted when a derived class tried to register another database provider like SQL Server.

Error message:
```
Services for database providers 'Microsoft.EntityFrameworkCore.SqlServer', 'Microsoft.EntityFrameworkCore.Sqlite' have been registered in the service provider. Only a single database provider can be registered in a service provider.
```

## Solution

This fix provides a solution by centralizing database provider registration to ensure only one database provider is registered at a time:

1. Added a `DatabaseProviderOptionsProvider` class that encapsulates database provider configuration
2. Created extension methods to register and use this provider
3. Updated both web applications to use a single database provider approach
4. Added an example custom persistence feature to show how to use this approach

## How to Use

### Registering the Single Database Provider

In your application startup, register the single database provider:

```csharp
services.AddSingleDatabaseProvider(sqlDatabaseProvider);
```

### Creating Custom Persistence Features

When creating a custom persistence feature that needs database access:

```csharp
public class MyCustomPersistenceFeature : FeatureBase
{
    public override void Apply()
    {
        Services.AddDbContext<MyDbContext>((sp, options) =>
        {
            // Get the provider options from DI
            var dbProviderOptions = sp.GetRequiredService<DatabaseProviderOptionsProvider>();
            
            // Configure the DbContext with the single provider
            dbProviderOptions.Configure(options);
        });
    }
}
```

This approach ensures that only one database provider is registered, preventing conflicts between SQLite and SQL Server or other providers.

See the `ExampleCustomPersistenceFeature.cs` file for a complete example.

## Files Added

1. `DatabaseProviderOptionsProvider.cs` - Core provider that ensures single database registration
2. `PersistenceFeatureExtensions.cs` - Extension methods for database configuration
3. `SingleDatabaseProviderServiceExtensions.cs` - DI registration extensions
4. `ExampleCustomPersistenceFeature.cs` - Example implementation
5. `README.md` - Documentation on the solution

## Benefits

- Eliminates conflicts between database providers
- Makes it easier to create custom persistence features
- Centralizes database provider configuration
- Works with all supported database providers (SQL Server, SQLite, PostgreSQL, etc.)
- Minimal changes to existing code