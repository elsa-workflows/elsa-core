using System.Diagnostics.CodeAnalysis;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Writes workflow dispatch commands to a durable outbox while a workflow execution is in progress.
/// </summary>
public class TransactionalWorkflowDispatcher(
    IWorkflowDispatcher decoratedService,
    IServiceProvider serviceProvider,
    IWorkflowDispatchOutboxAccessor outboxAccessor,
    IIdentityGenerator identityGenerator,
    IOptions<WorkflowDispatcherOptions> options,
    ITenantAccessor? tenantAccessor = null) : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        var generatedInstanceId = string.IsNullOrWhiteSpace(request.InstanceId) ? identityGenerator.GenerateId() : null;

        await GetOutbox().EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Channel = dispatchOptions?.Channel,
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = WorkflowDispatchCommandFactory.CreateCommand(request, generatedInstanceId)
        }, cancellationToken);

        // Dispatching/Dispatched notifications are sent by the inner dispatcher when the outbox delivers the item.
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await GetOutbox().EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Channel = dispatchOptions?.Channel,
            Kind = WorkflowDispatchOutboxItemKind.WorkflowInstance,
            WorkflowInstanceCommand = WorkflowDispatchCommandFactory.CreateCommand(request)
        }, cancellationToken);

        // Dispatching/Dispatched notifications are sent by the inner dispatcher when the outbox delivers the item.
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? dispatchOptions = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkflowExecutionContext(out var context))
            return await decoratedService.DispatchAsync(request, dispatchOptions, cancellationToken);

        await GetOutbox().EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Channel = dispatchOptions?.Channel,
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

        await GetOutbox().EnqueueAsync(context, new()
        {
            TenantId = tenantAccessor?.Tenant?.Id,
            Channel = dispatchOptions?.Channel,
            Kind = WorkflowDispatchOutboxItemKind.ResumeWorkflows,
            ResumeWorkflowsCommand = WorkflowDispatchCommandFactory.CreateCommand(request)
        }, cancellationToken);

        return DispatchWorkflowResponse.Success();
    }

    private bool TryGetWorkflowExecutionContext([NotNullWhen(true)] out WorkflowExecutionContext? context)
    {
        var currentContext = outboxAccessor.WorkflowExecutionContext;
        if (!options.Value.UseTransactionalOutbox || currentContext == null)
        {
            context = null;
            return false;
        }

        context = currentContext;
        return true;
    }

    private IWorkflowDispatchOutbox GetOutbox() => serviceProvider.GetRequiredService<IWorkflowDispatchOutbox>();
}
