using System.Net.Mime;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition.
/// </summary>
[PublicAPI]
internal class Execute(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRuntime workflowRuntime,
    IApiSerializer apiSerializer)
    : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/workflow-definitions/{definitionId}/execute");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("exec:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
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
        var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowGraph.Workflow.Identity.Id),
            CorrelationId = request.CorrelationId,
            Input = (IDictionary<string, object>?)request.Input
        };
        await workflowClient.CreateInstanceAsync(createWorkflowInstanceRequest, cancellationToken);
        var instanceId = workflowClient.WorkflowInstanceId;
        
        // Write the workflow instance ID to the response header.
        // This allows clients to read the header even if the workflow writes a response body
        // (in which case, we can't transmit a JSON body that includes the instance ID). 
        HttpContext.Response.Headers.Append("x-elsa-workflow-instance-id", instanceId);
        
        var runWorkflowInstanceRequest = new RunWorkflowInstanceRequest
        {
            TriggerActivityId = request.TriggerActivityId
        };
        
        // Start the workflow.
        var runWorkflowInstanceResponse = await workflowClient.RunAsync(runWorkflowInstanceRequest, cancellationToken);

        // If a workflow fault occurred, respond appropriately with a 500 internal server error.
        if (runWorkflowInstanceResponse.SubStatus == WorkflowSubStatus.Faulted)
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

    private async Task HandleFaultAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var faultedResponse = apiSerializer.Serialize(new Response(workflowState));

        HttpContext.Response.ContentType = MediaTypeNames.Application.Json;
        HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await HttpContext.Response.WriteAsync(faultedResponse, cancellationToken);
    }
}