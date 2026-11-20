using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class ExecutionCycleTrackingMiddlewareTests
{
    [Theory(DisplayName = "A custom registry is handed the restart point to set before it lists the handle, when InterruptOnHostStop is on")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CustomRegistryIsHandedRestartPoint(bool interruptOnHostStop)
    {
        var registry = new ListingRegistry();
        var context = (await new ActivityTestFixture(new WriteLine("test"))
            .ConfigureServices(services => services.Configure<GracefulShutdownOptions>(o => o.InterruptOnHostStop = interruptOnHostStop))
            .BuildAsync()).WorkflowExecutionContext;

        await new ExecutionCycleTrackingMiddleware(_ => ValueTask.CompletedTask, registry).InvokeAsync(context);

        Assert.Equal([interruptOnHostStop], registry.ConfiguredBeforeListing);
    }

    /// <summary>A registry that lists each handle as soon as it registers it, as a custom registry may.</summary>
    private sealed class ListingRegistry : IExecutionCycleRegistry
    {
        public List<bool> ConfiguredBeforeListing { get; } = [];
        public int ActiveCount => 0;
        public IReadOnlyCollection<ExecutionCycleHandle> ListActiveCycles() => [];

        public ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback = null) =>
            new(Guid.NewGuid(), workflowInstanceId, ingressSourceName, DateTimeOffset.UtcNow, linkedToken, cancelCallback: cancelCallback);

        public ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback, Action<ExecutionCycleHandle>? configure)
        {
            var handle = BeginCycle(workflowInstanceId, ingressSourceName, linkedToken, cancelCallback);
            configure?.Invoke(handle);
            ConfiguredBeforeListing.Add(configure != null);
            return handle;
        }
    }
}
