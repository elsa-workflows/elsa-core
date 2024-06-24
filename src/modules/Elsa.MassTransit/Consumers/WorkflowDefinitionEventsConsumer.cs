using Elsa.MassTransit.Messages;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes messages related to workflow definition changes.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionEventsConsumer(IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater) :
    IConsumer<WorkflowDefinitionDeleted>,
    IConsumer<WorkflowDefinitionPublished>,
    IConsumer<WorkflowDefinitionRetracted>,
    IConsumer<WorkflowDefinitionVersionRetracted>,
    IConsumer<WorkflowDefinitionsDeleted>,
    IConsumer<WorkflowDefinitionVersionDeleted>,
    IConsumer<WorkflowDefinitionVersionsDeleted>,
    IConsumer<WorkflowDefinitionVersionsUpdated>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionDeleted> context)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(context.Message.Id);
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
        return UpdateDefinition(context.Message.Id, context.Message.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionRetracted> context)
    {
        return UpdateDefinition(context.Message.Id, context.Message.UsableAsActivity);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionsDeleted> context)
    {
        foreach (var id in context.Message.Ids)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionDeleted> context)
    {
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(context.Message.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<WorkflowDefinitionVersionsDeleted> context)
    {
        foreach (var id in context.Message.Ids)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<WorkflowDefinitionVersionsUpdated> context)
    {
        foreach (WorkflowDefinitionVersionUpdate definitionUpdate in context.Message.WorkflowDefinitionVersionUpdates)
        {
            await UpdateDefinition(definitionUpdate.Id, definitionUpdate.UsableAsActivity);
        }
    }

    private Task UpdateDefinition(string id, bool usableAsActivity)
    {
        // Once a workflow has been published it should remain in the activity registry unless no longer being marked as an activity.
        if (usableAsActivity)
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(id);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        return Task.CompletedTask;
    }
}