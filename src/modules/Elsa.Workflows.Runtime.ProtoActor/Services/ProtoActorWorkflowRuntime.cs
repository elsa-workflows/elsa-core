using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ProtoActor.Services;

/// <summary>
/// Represents a Proto.Actor implementation of the workflows runtime.
/// </summary>
public partial class ProtoActorWorkflowRuntime(
    IServiceProvider serviceProvider, 
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowActivationStrategyEvaluator workflowActivationStrategyEvaluator,
    IStimulusSender stimulusSender,
    IStimulusHasher stimulusHasher,
    ITriggerBoundWorkflowService triggerBoundWorkflowService,
    IBookmarkBoundWorkflowService bookmarkBoundWorkflowService,
    IBookmarkStore bookmarkStore,
    IWorkflowInstanceStore workflowInstanceStore,
    IIdentityGenerator identityGenerator) : IWorkflowRuntime
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