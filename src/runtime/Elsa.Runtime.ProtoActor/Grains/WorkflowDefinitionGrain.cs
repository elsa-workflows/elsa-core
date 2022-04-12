using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Protos;
using Elsa.State;
using Proto;

namespace Elsa.Runtime.ProtoActor.Grains;

/// <summary>
/// Instantiates and executes a workflow instance for execution.
/// </summary>
public class WorkflowDefinitionGrain : WorkflowDefinitionGrainBase
{
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly ICommandSender _commandSender;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    public WorkflowDefinitionGrain(
        IWorkflowRegistry workflowRegistry,
        ICommandSender commandSender,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock,
        IContext context) : base(context)
    {
        _workflowRegistry = workflowRegistry;
        _commandSender = commandSender;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    public override async Task<ExecuteWorkflowDefinitionResponse> Execute(ExecuteWorkflowDefinitionRequest request)
    {
        var workflowDefinitionId = request.Id;
        var correlationId = request.CorrelationId;
        var cancellationToken = Context.CancellationToken;
        var workflowInstance = await CreateWorkflowInstanceAsync(workflowDefinitionId, correlationId, cancellationToken);

        var executeWorkflowInstanceMessage = new ExecuteWorkflowInstanceRequest
        {
            Id = workflowInstance.Id,
            Input = request.Input,
            CorrelationId = correlationId
        };

        var workflowInstanceGrainClient = Context.GetWorkflowInstanceGrain(workflowInstance.Id);
        var workflowInstanceResponse = await workflowInstanceGrainClient.Execute(executeWorkflowInstanceMessage, CancellationTokens.FromSeconds(1000));

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
        var workflow = (await _workflowRegistry.FindByIdAsync(workflowDefinitionId, VersionOptions.Published, cancellationToken))!;
        var workflowInstanceId = _identityGenerator.GenerateId();

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = _identityGenerator.GenerateId();
        
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            Version = workflow.Identity.Version,
            DefinitionId = workflowDefinitionId,
            DefinitionVersionId = workflow.Identity.Id,
            CorrelationId =  correlationId,
            CreatedAt = _systemClock.UtcNow,
            WorkflowStatus = WorkflowStatus.Idle,
            WorkflowState = new WorkflowState
            {
                Id = workflowInstanceId
            }
        };

        await _commandSender.ExecuteAsync(new SaveWorkflowInstance(workflowInstance), cancellationToken);
        return workflowInstance;
    }
}