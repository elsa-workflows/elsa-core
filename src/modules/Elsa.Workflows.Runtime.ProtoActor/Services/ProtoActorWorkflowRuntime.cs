using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ProtoActor.Services;

/// <summary>
/// Represents a Proto.Actor implementation of the workflows runtime.
/// </summary>
public partial class ProtoActorWorkflowRuntime : IWorkflowRuntime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Represents a Proto.Actor implementation of the workflows runtime.
    /// </summary>
    public ProtoActorWorkflowRuntime(IServiceProvider serviceProvider, 
        IIdentityGenerator identityGenerator)
    {
        _serviceProvider = serviceProvider;
        _identityGenerator = identityGenerator;
        _obsoleteApi = ActivatorUtilities.CreateInstance<ObsoleteWorkflowRuntime>(serviceProvider, (Func<string?, CancellationToken, ValueTask<IWorkflowClient>>)CreateClientAsync);
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
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(ProtoActorWorkflowClient), workflowInstanceId);
        return new(client);
    }
}