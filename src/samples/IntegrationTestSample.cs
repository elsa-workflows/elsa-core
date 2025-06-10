using System;
using System.Threading.Tasks;
using Elsa.Testing.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sample;

/// <summary>
/// Sample integration test showing how to properly shutdown Elsa background tasks.
/// </summary>
/// <typeparam name="TStartup">The startup class of your application.</typeparam>
public class IntegrationTestBase<TStartup> : IAsyncDisposable where TStartup : class
{
    protected readonly WebApplicationFactory<TStartup> Factory;

    public IntegrationTestBase()
    {
        Factory = new WebApplicationFactory<TStartup>();
        // ... other initialization code
    }

    public async ValueTask DisposeAsync()
    {
        // Explicitly stop Elsa background tasks before disposing the factory
        await Factory.ShutdownElsaAsync();
        
        // Then stop the server and host
        if (Factory.Server?.Host != null)
        {
            await Factory.Server.Host.StopAsync();
        }
        
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Example test class inheriting from the base integration test class.
/// </summary>
public class ExampleIntegrationTest : IntegrationTestBase<TestStartup>
{
    [Fact]
    public async Task SampleTest()
    {
        // Test logic here
        await Task.CompletedTask;
    }
}

/// <summary>
/// Sample startup class for demonstration purposes.
/// </summary>
public class TestStartup
{
    // This is just a placeholder class for the example
}