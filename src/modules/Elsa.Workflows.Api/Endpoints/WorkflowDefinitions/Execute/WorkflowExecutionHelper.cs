using System.Net.Mime;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public static class WorkflowExecutionHelper
{
    public static async Task ExecuteWorkflowAsync(
        IExecutionRequest request,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRuntime workflowRuntime,
        IWorkflowStarter workflowStarter,
        IApiSerializer apiSerializer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = request.VersionOptions ?? VersionOptions.Published;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);

        if (workflowGraph == null)
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }
        
        var startRequest = new StartWorkflowRequest
        {
            Workflow = workflowGraph.Workflow,
            CorrelationId = request.CorrelationId,
            Name = request.Name,
            Input = request.GetInputAsDictionary(),
            Variables = request.GetVariablesAsDictionary(),
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle
        };
        
        var startResponse = await workflowStarter.StartWorkflowAsync(startRequest, cancellationToken);
        
        if(!httpContext.Response.HasStarted)
            httpContext.Response.Headers.Append("x-elsa-workflow-cannot-start", startResponse.CannotStart.ToString());
        
        if (startResponse.CannotStart)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.SendOkAsync(cancellationToken);
            return;
        }

        var instanceId = startResponse.WorkflowInstanceId!;
        
        if(!httpContext.Response.HasStarted)
            httpContext.Response.Headers.Append("x-elsa-workflow-instance-id", instanceId);
        
        var workflowClient = await workflowRuntime.CreateClientAsync(instanceId, cancellationToken);

        if (startResponse.SubStatus == WorkflowSubStatus.Faulted)
        {
            var workflowState = await workflowClient.ExportStateAsync(cancellationToken);
            await HandleFaultAsync(workflowState, apiSerializer, httpContext, cancellationToken);
        }
        else
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.Headers.Append("x-elsa-response", "true");
                if (httpContext.Response.StatusCode == StatusCodes.Status200OK)
                {
                    var workflowState = await workflowClient.ExportStateAsync(cancellationToken);
                    var response = apiSerializer.Serialize(new Response(workflowState));
                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    await httpContext.Response.WriteAsync(response, cancellationToken);
                }
            }
        }
    }

    private static async Task HandleFaultAsync(WorkflowState workflowState, IApiSerializer apiSerializer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var faultedResponse = apiSerializer.Serialize(new Response(workflowState));
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsync(faultedResponse, cancellationToken);
    }
}

