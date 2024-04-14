using Elsa.Workflows.Runtime.Distributed.Contracts;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.Services;

/// <summary>
/// Represents a local implementation of the distributed runtime for running workflows.
/// It does not support clustering and is intended for single-node deployments only.
/// For distributed deployments, use Proto.Actor or another distributed runtime.
/// </summary>
public class LocalDistributedRuntime(IServiceProvider serviceProvider) : IDistributedRuntime
{
    /// <inheritdoc />
    public ValueTask<IWorkflowClient> CreateClientAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, typeof(LocalWorkflowClient), workflowInstanceId);
        return new(client);
    }
}