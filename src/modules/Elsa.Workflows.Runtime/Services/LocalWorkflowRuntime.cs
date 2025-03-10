using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a local implementation of the distributed runtime for running workflows.
/// It does not support clustering and is intended for single-node deployments only.
/// For distributed deployments, use Proto.Actor or another distributed runtime.
/// </summary>
public partial class LocalWorkflowRuntime : IWorkflowRuntime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Represents a local implementation of the distributed runtime for running workflows.
    /// It does not support clustering and is intended for single-node deployments only.
    /// For distributed deployments, use Proto.Actor or another distributed runtime.
    /// </summary>
    public LocalWorkflowRuntime(IServiceProvider serviceProvider, IIdentityGenerator identityGenerator)
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
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(LocalWorkflowClient), workflowInstanceId);
        return new(client);
    }
}