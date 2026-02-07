using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class BackgroundWorkflowDispatcher(ICommandSender commandSender, INotificationSender notificationSender, ITenantAccessor tenantAccessor) : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Emit dispatching notification
        await notificationSender.SendAsync(new WorkflowDefinitionDispatching(request), cancellationToken);
        
        var command = new DispatchWorkflowDefinitionCommand(request.DefinitionVersionId)
        {
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            InstanceId = request.InstanceId,
            TriggerActivityId = request.TriggerActivityId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = request.SchedulingCallStackDepth
        };
        
        await commandSender.SendAsync(command, CommandStrategy.Background, CreateHeaders(), cancellationToken);
        var response = DispatchWorkflowResponse.Success();
        
        // Emit dispatched notification
        await notificationSender.SendAsync(new WorkflowDefinitionDispatched(request, response), cancellationToken);
        
        return response;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Emit dispatching notification
        await notificationSender.SendAsync(new WorkflowInstanceDispatching(request), cancellationToken);
        
        var command = new DispatchWorkflowInstanceCommand(request.InstanceId){
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId};

        await commandSender.SendAsync(command, CommandStrategy.Background, CreateHeaders(), cancellationToken);
        var response = DispatchWorkflowResponse.Success();
        
        // Emit dispatched notification
        await notificationSender.SendAsync(new WorkflowInstanceDispatched(request, response), cancellationToken);
        
        return response;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        var command = new DispatchTriggerWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input,
            Properties = request.Properties
        };
        await commandSender.SendAsync(command, CommandStrategy.Background, CreateHeaders(), cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        };
        await commandSender.SendAsync(command, CommandStrategy.Background, CreateHeaders(), cancellationToken);
        return DispatchWorkflowResponse.Success();
    }
    
    private IDictionary<object, object> CreateHeaders()
    {
        return TenantHeaders.CreateHeaders(tenantAccessor.Tenant?.Id);
    }
}