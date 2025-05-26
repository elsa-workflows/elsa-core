# Elsa Testing Extensions

This package provides extensions to help with testing Elsa Workflows applications.

## WebApplicationFactory Extensions

When using ASP.NET Core's `WebApplicationFactory<TEntryPoint>` for integration testing with Elsa Workflows, background tasks might continue running after tests complete. This can cause the test host to crash due to attempts to access resources (like databases) that have been disposed.

### ShutdownElsaAsync

The `ShutdownElsaAsync` extension method ensures proper cleanup by:

1. Accessing the tenant service
2. Deactivating all tenants, which in turn:
   - Cancels all recurring tasks
   - Stops all background tasks
   - Cleans up resources properly

Example usage in integration tests:

```csharp
public class MyIntegrationTest : IAsyncDisposable
{
    protected WebApplicationFactory<Startup> Factory;

    public MyIntegrationTest()
    {
        Factory = new WebApplicationFactory<Startup>();
        // Test initialization
    }

    public async ValueTask DisposeAsync()
    {
        // Explicitly shutdown Elsa before stopping the host
        await Factory.ShutdownElsaAsync();
        
        // Then stop the server
        if (Factory.Server?.Host != null)
            await Factory.Server.Host.StopAsync();
        
        Factory.Dispose();
    }
}
```

This ensures all background tasks are properly stopped before the test host is shut down, preventing errors like database connection failures after database disposal.