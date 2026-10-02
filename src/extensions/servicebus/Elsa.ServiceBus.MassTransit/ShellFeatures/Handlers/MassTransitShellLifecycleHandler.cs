using CShells.Lifecycle;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures.Handlers;

/// <summary>
/// Handles MassTransit bus lifecycle when a shell is activated or deactivated.
/// </summary>
/// <remarks>
/// <para>
/// In CShells architecture, each shell has its own isolated service collection and provider.
/// MassTransit's hosted service (which normally starts the bus) is registered in the shell's
/// container, but only hosted services in the root container are started by the application host.
/// </para>
/// <para>
/// This handler bridges that gap by manually starting the bus when the shell activates
/// and stopping it when the shell deactivates, providing proper lifecycle management
/// for shell-scoped MassTransit instances.
/// </para>
/// </remarks>
public class MassTransitShellLifecycleHandler(IBus bus, ILogger<MassTransitShellLifecycleHandler> logger) : IShellInitializer, IDrainHandler
{
    private readonly IBusControl _busControl = (IBusControl)bus;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting MassTransit bus for shell");
            await _busControl.StartAsync(cancellationToken);
            logger.LogInformation("MassTransit bus started successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start MassTransit bus for shell");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Stopping MassTransit bus for shell");
            await _busControl.StopAsync(cancellationToken);
            logger.LogInformation("MassTransit bus stopped successfully");
        }
        catch (Exception ex)
        {
            // Log but don't throw during shutdown to allow other cleanup to proceed
            logger.LogError(ex, "Failed to stop MassTransit bus for shell");
        }
    }
}
