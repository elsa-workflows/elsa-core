using Elsa.Common;

namespace Elsa.Common.ShellHandlers;

public sealed class MediatorBackgroundTask(MediatorBackgroundProcessingCoordinator coordinator) : BackgroundTask
{
    public override Task StartAsync(CancellationToken cancellationToken) => coordinator.StartAsync(cancellationToken);

    public override Task StopAsync(CancellationToken cancellationToken) => coordinator.StopAsync(cancellationToken);
}
