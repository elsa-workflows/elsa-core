# Single Database Provider Registration

This folder contains extensions that help prevent conflicts when registering multiple database providers with Entity Framework Core.

## Problem

The issue occurs when creating custom persistence features that inherit from `PersistenceFeatureBase`. By default, this base class registers SQLite as a database provider, which causes conflicts when a derived class tries to register another database provider like SQL Server.

The error you might see is:

```
Services for database providers 'Microsoft.EntityFrameworkCore.SqlServer', 'Microsoft.EntityFrameworkCore.Sqlite' have been registered in the service provider. Only a single database provider can be registered in a service provider.
```

## Solution

This extension provides a way to ensure only one database provider is registered at a time by using a centralized configuration approach:

1. `DatabaseProviderOptionsProvider` - Encapsulates the database provider selection
2. `PersistenceFeatureExtensions` - Contains extension methods to configure a specific database provider
3. `SingleDatabaseProviderServiceExtensions` - Extension methods to register the single provider approach with dependency injection

## Usage

To use this approach in your application:

1. Register the single database provider at application startup:

```csharp
services.AddSingleDatabaseProvider(sqlDatabaseProvider);
```

2. When creating a custom persistence feature, instead of directly calling database provider registration methods, use:

```csharp
optionsBuilder.ConfigureSingleDatabaseProvider(databaseProvider, serviceProvider);
```

This ensures that only one database provider is registered, preventing conflicts between SQLite and other providers.

## Custom Feature Example

When creating a custom persistence feature, you can use this pattern to avoid conflicts:

```csharp
public class MyCustomPersistenceFeature : PersistenceFeatureBase
{
    public override void Apply()
    {
        base.Apply();
        
        // Get the database provider options from DI
        var dbProviderOptions = Services.GetRequiredService<DatabaseProviderOptionsProvider>();
        
        // Register with your custom configuration
        Services.AddDbContext<MyDbContext>((sp, options) => 
        {
            dbProviderOptions.Configure(options);
            // Add other configuration as needed
        });
    }
}
```

This approach ensures that only one database provider is registered, avoiding the conflicts between SQLite and SQL Server.