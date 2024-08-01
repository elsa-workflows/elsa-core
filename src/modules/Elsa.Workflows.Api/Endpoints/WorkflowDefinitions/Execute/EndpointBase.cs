using System.Net.Mime;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// This abstract class provides the necessary infrastructure to handle the execution of workflows, including setup of routes, permissions,
/// and processing of HTTP requests to execute workflows.
/// </summary>
internal abstract class EndpointBase<T>(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRuntime workflowRuntime,
    IApiSerializer apiSerializer)
    : ElsaEndpoint<T, Response> where T : IExecutionRequest, new()
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/workflow-definitions/{definitionId}/execute");
        ConfigurePermissions("exec:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(T request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = request.VersionOptions ?? VersionOptions.Published;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);

        if (workflowGraph == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }
        
        var workflowClient = await workflowRuntime.CreateClientAsync(cancellationToken);
        var createWorkflowInstanceRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowGraph.Workflow.Identity.Id),
            CorrelationId = request.CorrelationId,
            Input = GetInput(request),
            TriggerActivityId = request.TriggerActivityId
        };
        
        // Create and run the workflow instance.
        var runWorkflowResponse = await workflowClient.CreateAndRunInstanceAsync(createWorkflowInstanceRequest, cancellationToken);
        var instanceId = workflowClient.WorkflowInstanceId;
        
        // Write the workflow instance ID to the response header.
        // This allows clients to read the header even if the workflow writes a response body
        // (in which case, we can't transmit a JSON body that includes the instance ID). 
        HttpContext.Response.Headers.Append("x-elsa-workflow-instance-id", instanceId);
        
        // If a workflow fault occurred, respond appropriately with a 500 internal server error.
        if (runWorkflowResponse.SubStatus == WorkflowSubStatus.Faulted)
        {
            var workflowState = await workflowClient.ExportStateAsync(cancellationToken);
            await HandleFaultAsync(workflowState, cancellationToken);
        }
        else
        {
            if (!HttpContext.Response.HasStarted)
            {
                // Write a response header to indicate that the response is a workflow state response.
                // This is used by tools like Elsa Studio to determine if the response is in response to a workflow execution manually triggered by the user.
                HttpContext.Response.Headers.Append("x-elsa-response", "true");

                // Only write a response if the workflow didn't change the HTTP status code.
                if (HttpContext.Response.StatusCode == StatusCodes.Status200OK)
                {
                    var workflowState = await workflowClient.ExportStateAsync(cancellationToken);
                    await SendOkAsync(new Response(workflowState), cancellationToken);
                }
            }
        }
    }

    protected abstract IDictionary<string, object>? GetInput(T request);

    private async Task HandleFaultAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var faultedResponse = apiSerializer.Serialize(new Response(workflowState));

        HttpContext.Response.ContentType = MediaTypeNames.Application.Json;
        HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await HttpContext.Response.WriteAsync(faultedResponse, cancellationToken);
    }
}