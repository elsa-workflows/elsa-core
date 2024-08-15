using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Represents a distributed workflow runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance.
/// </summary>
public class DistributedWorkflowRuntime(IServiceProvider serviceProvider, IIdentityGenerator identityGenerator) : IWorkflowRuntime
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
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, typeof(DistributedWorkflowClient), workflowInstanceId);
        return new(client);
    }
}