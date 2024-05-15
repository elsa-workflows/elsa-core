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
    public Task Consume(ConsumeContext<WorkflowDefinitionCreated> context)
    {
        return UpdateDefinition(context.Message.Id, context.Message.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionDeleted> context)
    { 
        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), context.Message.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionPublished> context)
    {
        return UpdateDefinition(context.Message.Id, context.Message.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionRetracted> context)
    {
        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), context.Message.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionsDeleted> context)
    {
        foreach (var id in context.Message.Ids)
        {
            activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionDeleted> context)
    {
        activityRegistryPopulator.RemoveDefinitionVersionFromRegistry(typeof(WorkflowDefinitionActivityProvider), context.Message.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionsDeleted> context)
    {
        foreach (var id in context.Message.Ids)
        {
            activityRegistryPopulator.RemoveDefinitionVersionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id);
        }

        return Task.CompletedTask;
    }

    private Task UpdateDefinition(string id, bool usableAsActivity)
    {
        if (usableAsActivity)
            return activityRegistryPopulator.AddToRegistry(typeof(WorkflowDefinitionActivityProvider), id);

        activityRegistryPopulator.RemoveDefinitionFromRegistry(typeof(WorkflowDefinitionActivityProvider), id);
        return Task.CompletedTask;
    }
}