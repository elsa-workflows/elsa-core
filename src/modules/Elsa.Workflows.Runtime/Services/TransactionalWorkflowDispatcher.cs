using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Writes workflow dispatch commands to a durable outbox while a workflow execution is in progress.
/// </summary>
public class TransactionalWorkflowDispatcher(
    IWorkflowDispatcher decoratedService,
    IWorkflowDispatchOutbox outbox,
    IWorkflowDispatchOutboxAccessor outboxAccessor,
    INotificationSender notificationSender,
    IIdentityGenerator identityGenerator,
    IOptions<WorkflowDispatcherOptions> options,
    ITenantAccessor? tenantAccessor = null) : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await notificationSender.SendAsync(new WorkflowDefinitionDispatching(request), cancellationToken);
        var generatedInstanceId = string.IsNullOrWhiteSpace(request.InstanceId) ? identityGenerator.GenerateId() : null;

        await outbox.EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = WorkflowDispatchCommandFactory.CreateCommand(request, generatedInstanceId)
        }, cancellationToken);

        var response = DispatchWorkflowResponse.Success();
        await notificationSender.SendAsync(new WorkflowDefinitionDispatched(request, response), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await notificationSender.SendAsync(new WorkflowInstanceDispatching(request), cancellationToken);

        await outbox.EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Kind = WorkflowDispatchOutboxItemKind.WorkflowInstance,
            WorkflowInstanceCommand = WorkflowDispatchCommandFactory.CreateCommand(request)
        }, cancellationToken);

        var response = DispatchWorkflowResponse.Success();
        await notificationSender.SendAsync(new WorkflowInstanceDispatched(request, response), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await outbox.EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Kind = WorkflowDispatchOutboxItemKind.TriggerWorkflows,
            TriggerWorkflowsCommand = WorkflowDispatchCommandFactory.CreateCommand(request)
        }, cancellationToken);

        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await outbox.EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Kind = WorkflowDispatchOutboxItemKind.ResumeWorkflows,
            ResumeWorkflowsCommand = WorkflowDispatchCommandFactory.CreateCommand(request)
        }, cancellationToken);

        return DispatchWorkflowResponse.Success();
    }

    private bool TryGetWorkflowExecutionContext(out WorkflowExecutionContext context)
    {
        context = outboxAccessor.WorkflowExecutionContext!;
        return options.Value.UseTransactionalOutbox && context != null;
    }
}
