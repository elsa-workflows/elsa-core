using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ProtoActor.Services;

/// <summary>
/// Represents a Proto.Actor implementation of the distributed runtime for running workflows.
/// </summary>
public class ProtoActorDistributedRuntime(IServiceProvider serviceProvider, IIdentityGenerator identityGenerator) : IWorkflowRuntime
{
    /// <inheritdoc />
    public async ValueTask<IWorkflowClient> CreateClientAsync(CancellationToken cancellationToken = default)
    {
        return await CreateClientAsync(null, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IWorkflowClient> CreateClientAsync(string? workflowInstanceId, CancellationToken cancellationToken = default)
    {
        workflowInstanceId ??= identityGenerator.GenerateId();
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ProtoActorWorkflowClient), workflowInstanceId);
        return new(client);
    }
}