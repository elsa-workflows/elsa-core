using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Represents a distributed workflow runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance.
/// </summary>
public partial class DistributedWorkflowRuntime : IWorkflowRuntime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Represents a distributed workflow runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance.
    /// </summary>
    public DistributedWorkflowRuntime(IServiceProvider serviceProvider, IIdentityGenerator identityGenerator)
    {
        _serviceProvider = serviceProvider;
        _identityGenerator = identityGenerator;
        _obsoleteApi = new(() => ObsoleteWorkflowRuntime.Create(serviceProvider, CreateClientAsync));
    }

    /// <inheritdoc />
    public async ValueTask<IWorkflowClient> CreateClientAsync(CancellationToken cancellationToken = default)
    {
        return await CreateClientAsync(null, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IWorkflowClient> CreateClientAsync(string? workflowInstanceId, CancellationToken cancellationToken = default)
    {
        workflowInstanceId ??= _identityGenerator.GenerateId();
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(DistributedWorkflowClient), workflowInstanceId);
        return new(client);
    }
}