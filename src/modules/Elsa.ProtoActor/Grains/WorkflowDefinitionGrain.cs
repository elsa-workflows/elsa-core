using System;
using System.Threading.Tasks;
using Elsa.Runtime.Protos;
using Elsa.Services;
using Proto;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Instantiates and executes a workflow instance for execution.
/// </summary>
public class WorkflowDefinitionGrain : WorkflowDefinitionGrainBase
{
    private readonly IIdentityGenerator _identityGenerator;

    public WorkflowDefinitionGrain(IIdentityGenerator identityGenerator, IContext context) : base(context)
    {
        _identityGenerator = identityGenerator;
    }

    public override async Task<ExecuteWorkflowDefinitionResponse> Execute(ExecuteWorkflowDefinitionRequest request)
    {
        var definitionId = request.Id;
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var cancellationToken = Context.CancellationToken;
        var workflowInstanceId = _identityGenerator.GenerateId();

        var executeWorkflowRequest = new ExecuteNewWorkflowInstanceRequest
        {
            DefinitionId = definitionId,
            VersionOptions = request.VersionOptions,
            CorrelationId = correlationId ?? "",
            Input = request.Input
        };

        var workflowInstanceGrainClient = Context.GetWorkflowInstanceGrain(workflowInstanceId);
        var workflowInstanceResponse = await workflowInstanceGrainClient.ExecuteNewInstance(executeWorkflowRequest, cancellationToken);

        if (workflowInstanceResponse == null)
            throw new TimeoutException("Did not receive a response from the WorkflowInstance actor within the configured amount of time.");

        var response = new ExecuteWorkflowDefinitionResponse
        {
            WorkflowState = workflowInstanceResponse.WorkflowState,
        };

        response.Bookmarks.AddRange(workflowInstanceResponse.Bookmarks);
        return response;
    }
}