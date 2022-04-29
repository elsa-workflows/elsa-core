using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Protos;
using Elsa.Runtime.Services;
using Proto;

namespace Elsa.Runtime.ProtoActor.Grains;

/// <summary>
/// Instantiates and executes a workflow instance for execution.
/// </summary>
public class WorkflowDefinitionGrain : WorkflowDefinitionGrainBase
{
    private readonly ICommandSender _commandSender;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;

    public WorkflowDefinitionGrain(
        ICommandSender commandSender,
        IWorkflowInstanceFactory workflowInstanceFactory,
        IContext context) : base(context)
    {
        _commandSender = commandSender;
        _workflowInstanceFactory = workflowInstanceFactory;
    }

    public override async Task<ExecuteWorkflowDefinitionResponse> Execute(ExecuteWorkflowDefinitionRequest request)
    {
        var workflowDefinitionId = request.Id;
        var correlationId = request.CorrelationId;
        var cancellationToken = Context.CancellationToken;
        var workflowInstance = await CreateWorkflowInstanceAsync(workflowDefinitionId, correlationId, cancellationToken);

        var executeWorkflowInstanceMessage = new ExecuteExistingWorkflowInstanceRequest
        {
            InstanceId = workflowInstance.Id,
            Input = request.Input,
            CorrelationId = correlationId
        };

        var workflowInstanceGrainClient = Context.GetWorkflowInstanceGrain(workflowInstance.Id);
        var workflowInstanceResponse = await workflowInstanceGrainClient.ExecuteExistingInstance(executeWorkflowInstanceMessage, CancellationTokens.FromSeconds(1000));

        if (workflowInstanceResponse == null)
            throw new TimeoutException("Did not receive a response from the WorkflowInstance actor within the configured amount of time.");

        var response = new ExecuteWorkflowDefinitionResponse
        {
            WorkflowState = workflowInstanceResponse.WorkflowState,
        };

        response.Bookmarks.AddRange(workflowInstanceResponse.Bookmarks);
        return response;
    }

    private async Task<WorkflowInstance> CreateWorkflowInstanceAsync(string workflowDefinitionId, string? correlationId, CancellationToken cancellationToken)
    {
        var workflowInstance = await _workflowInstanceFactory.CreateAsync(workflowDefinitionId, correlationId, cancellationToken);
        await _commandSender.ExecuteAsync(new SaveWorkflowInstance(workflowInstance), cancellationToken);
        return workflowInstance;
    }
}