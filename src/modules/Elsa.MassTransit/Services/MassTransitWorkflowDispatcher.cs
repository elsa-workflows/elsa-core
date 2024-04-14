using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Implements <see cref="IWorkflowDispatcher"/> by leveraging MassTransit.
/// </summary>
public class MassTransitWorkflowDispatcher(
    IBus bus,
    IEndpointChannelFormatter endpointChannelFormatter,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceManager workflowInstanceManager)
    : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // When a request is received to execute a workflow, the initial step taken by our system is the creation of the workflow instance.
        // The input parameters for the particular instance are immediately persisted in the database. This step is crucial for a couple of reasons:
        // 1. Size constraint: It helps us prevent scenarios where the message size exceeds limits. Large messages cause the system to fail at the sending stage.
        // 2. Performance: A smaller message size means less information needs to be processed and transferred, optimizing speed and efficiency.

        // To create the instance, we need to find the workflow definition first.
        var workflow = await workflowDefinitionService.FindWorkflowAsync(request.DefinitionId, request.VersionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception($"Workflow definition with definition ID '{request.DefinitionId} and version {request.VersionOptions}' not found");

        var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
        {
            Workflow = workflow,
            WorkflowInstanceId = request.InstanceId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId
        };

        // The workflow instance is created and persisted in the database.
        var workflowInstance = await workflowInstanceManager.CreateWorkflowInstanceAsync(createWorkflowInstanceRequest, cancellationToken);

        // The workflow instance is then dispatched for execution.
        var sendEndpoint = await GetSendEndpointAsync(options);
        var message = DispatchWorkflowDefinition.DispatchExistingWorkflowInstance(workflowInstance.Id, request.TriggerActivityId);
        await sendEndpoint.Send(message, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);

        await sendEndpoint.Send(new DispatchWorkflowInstance(request.InstanceId)
        {
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            CorrelationId = request.CorrelationId
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchTriggerWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchResumeWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    private async Task<ISendEndpoint> GetSendEndpointAsync(DispatchWorkflowOptions? options = default)
    {
        var endpointName = endpointChannelFormatter.FormatEndpointName(options?.Channel);
        var sendEndpoint = await bus.GetSendEndpoint(new Uri($"queue:{endpointName}"));
        return sendEndpoint;
    }
}