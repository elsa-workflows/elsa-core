using Elsa.MassTransit.Messages;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes messages related to workflow definition changes.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionEventsConsumer(IActivityRegistryPopulator activityRegistryPopulator) :
    IConsumer<WorkflowDefinitionCreated>,
    IConsumer<WorkflowDefinitionDeleted>,
    IConsumer<WorkflowDefinitionPublished>,
    IConsumer<WorkflowDefinitionRetracted>,
    IConsumer<WorkflowDefinitionsDeleted>,
    IConsumer<WorkflowDefinitionVersionDeleted>,
    IConsumer<WorkflowDefinitionVersionsDeleted>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionCreated> context) => RefreshAsync();
    
    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionDeleted> context) => RefreshAsync();

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<WorkflowDefinitionPublished> context)
    {
        await activityRegistryPopulator.AddToRegistry(typeof(WorkflowDefinitionActivityProvider), context.Message.Id);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionRetracted> context) => RefreshAsync();

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionsDeleted> context) => RefreshAsync();

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionDeleted> context) => RefreshAsync();

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionsDeleted> context) => RefreshAsync();
    
    private async Task RefreshAsync() => await activityRegistryPopulator.PopulateRegistryAsync(typeof(WorkflowDefinitionActivityProvider));
}