using Azure.Messaging.ServiceBus;

namespace Elsa.Workflows.ComponentTests.Helpers.Mocks;

public class MockServiceBusProcessor(Action onClose) : ServiceBusProcessor
{
    public Task RaiseProcessMessageAsync(ProcessMessageEventArgs args)
    {
        return base.OnProcessMessageAsync(args);
    }
    
    public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        // No-op.
        return Task.CompletedTask;
    }

    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await base.CloseAsync(cancellationToken);
        onClose();
    }
}