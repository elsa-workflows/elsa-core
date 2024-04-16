using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ProtoActor.Services;

/// <summary>
/// Represents a Proto.Actor implementation of the distributed runtime for running workflows.
/// </summary>
public class ProtoActorDistributedRuntime(IServiceProvider serviceProvider) : IDistributedRuntime
{
    /// <inheritdoc />
    public ValueTask<IWorkflowClient> CreateClientAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, typeof(LocalWorkflowClient), workflowInstanceId);
        return new(client);
    }
}