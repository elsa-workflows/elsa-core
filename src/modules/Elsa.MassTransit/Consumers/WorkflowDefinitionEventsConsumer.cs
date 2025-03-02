using Elsa.MassTransit.Messages;
using Elsa.MassTransit.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes messages related to workflow definition changes.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionEventsConsumer(IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater, INotificationSender notificationSender) :
    global::MassTransit.IConsumer<WorkflowDefinitionDeleted>,
    global::MassTransit.IConsumer<WorkflowDefinitionPublished>,
    global::MassTransit.IConsumer<WorkflowDefinitionRetracted>,
    global::MassTransit.IConsumer<WorkflowDefinitionVersionRetracted>,
    global::MassTransit.IConsumer<WorkflowDefinitionsDeleted>,
    global::MassTransit.IConsumer<WorkflowDefinitionVersionDeleted>,
    global::MassTransit.IConsumer<WorkflowDefinitionVersionsDeleted>,
    global::MassTransit.IConsumer<WorkflowDefinitionVersionsUpdated>,
    global::MassTransit.IConsumer<WorkflowDefinitionsRefreshed>,
    global::MassTransit.IConsumer<WorkflowDefinitionsReloaded>
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
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(id);

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
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<WorkflowDefinitionVersionsUpdated> context)
    {
        foreach (WorkflowDefinitionVersionUpdate definitionUpdate in context.Message.WorkflowDefinitionVersionUpdates)
            await UpdateDefinition(definitionUpdate.Id, definitionUpdate.UsableAsActivity);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<WorkflowDefinitionsRefreshed> context)
    {
        var message = context.Message;
        var notification = new Elsa.Workflows.Runtime.Notifications.WorkflowDefinitionsRefreshed(message.WorkflowDefinitionIds);
        AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer = true;
        await notificationSender.SendAsync(notification, context.CancellationToken);
        AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer = false;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<WorkflowDefinitionsReloaded> context)
    {
        var message = context.Message;
        var notification = new Elsa.Workflows.Runtime.Notifications.WorkflowDefinitionsReloaded(message.ReloadedWorkflowDefinitions);
        AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer = true;
        await notificationSender.SendAsync(notification, context.CancellationToken);
        AmbientConsumerScope.IsWorkflowDefinitionEventsConsumer = false;
    }

    private Task UpdateDefinition(string id, bool usableAsActivity)
    {
        // Once a workflow has been published, it should remain in the activity registry unless no longer being marked as an activity.
        if (usableAsActivity)
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(id);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        return Task.CompletedTask;
    }
}