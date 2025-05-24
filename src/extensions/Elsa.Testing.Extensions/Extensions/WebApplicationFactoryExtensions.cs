using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplicationFactory{TEntryPoint}"/> to help with proper Elsa shutdown.
/// </summary>
public static class WebApplicationFactoryExtensions
{
    /// <summary>
    /// Explicitly shuts down Elsa background tasks in integration tests.
    /// 
    /// This method should be called in integration tests before stopping or disposing the WebApplicationFactory
    /// to ensure all Elsa background and recurring tasks are properly stopped. This prevents errors
    /// when tests drop or dispose databases that might still be accessed by these background tasks.
    /// </summary>
    /// <remarks>
    /// When using ASP.NET Core's WebApplicationFactory for integration testing with Elsa,
    /// background tasks might continue running after tests complete, causing the test host
    /// to crash due to attempts to access disposed resources. This method ensures proper cleanup.
    /// 
    /// Example usage in test teardown:
    /// <code>
    /// public async ValueTask DisposeAsync()
    /// {
    ///     await Factory.ShutdownElsaAsync();
    ///     
    ///     if (Factory.Server?.Host != null)
    ///         await Factory.Server.Host.StopAsync();
    ///     
    ///     Factory.Dispose();
    /// }
    /// </code>
    /// </remarks>
    /// <typeparam name="TEntryPoint">The entry point of the web application.</typeparam>
    /// <param name="factory">The web application factory.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task ShutdownElsaAsync<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory) where TEntryPoint : class
    {
        using var scope = factory.Services.CreateScope();
        var tenantService = scope.ServiceProvider.GetService<ITenantService>();
        
        if (tenantService != null)
        {
            // Deactivating tenants will properly shutdown all recurring and background tasks
            await tenantService.DeactivateTenantsAsync();
        }
    }
}