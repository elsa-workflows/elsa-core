using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.ProtoActor.Messages;
using Elsa.State;
using Proto;
using Bookmark = Elsa.Runtime.ProtoActor.Messages.Bookmark;

namespace Elsa.Runtime.ProtoActor.Actors;

/// <summary>
/// Executes a workflow instance.
/// </summary>
public class WorkflowOperatorActor : IActor
{
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowOperatorActor(IRequestSender requestSender, IWorkflowRegistry workflowRegistry, IWorkflowEngine workflowEngine)
    {
        _requestSender = requestSender;
        _workflowRegistry = workflowRegistry;
        _workflowEngine = workflowEngine;
    }

    public Task ReceiveAsync(IContext context) => context.Message switch
    {
        ExecuteWorkflowInstance m => OnExecuteWorkflowInstance(context, m),
        _ => Task.CompletedTask
    };

    private async Task OnExecuteWorkflowInstance(IContext context, ExecuteWorkflowInstance message)
    {
        var workflowInstanceId = message.Id;
        var cancellationToken = context.CancellationToken;
        var workflowInstance = await _requestSender.RequestAsync(new FindWorkflowInstance(workflowInstanceId), cancellationToken);

        if (workflowInstance == null)
            throw new Exception($"No workflow instance found with ID {workflowInstanceId}");

        var workflowDefinitionId = workflowInstance.DefinitionId;
        var workflow = await _workflowRegistry.FindByIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
            throw new Exception($"No workflow definition found with ID {workflowDefinitionId}");

        var workflowState = workflowInstance.WorkflowState;
        var bookmarkMessage = message.Bookmark;

        var executionResult = await ExecuteAsync(workflow, workflowState, bookmarkMessage, cancellationToken);
        var response = MapResult(executionResult);

        context.Respond(response);
    }

    private ExecuteWorkflowResponse MapResult(ExecuteWorkflowResult result)
    {
        var bookmarks = result.Bookmarks.Select(x => new Bookmark
        {
            Id = x.Id,
            Hash = x.Hash,
            Payload = x.Payload,
            Name = x.Name,
            ActivityId = x.ActivityId,
            ActivityInstanceId = x.ActivityInstanceId,
            CallbackMethodName = x.CallbackMethodName
        });

        var response = new ExecuteWorkflowResponse
        {
            WorkflowState = new Json
            {
                Text = JsonSerializer.Serialize(result.WorkflowState)
            }
        };

        response.Bookmarks.Add(bookmarks);

        return response;
    }

    private async Task<ExecuteWorkflowResult> ExecuteAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmarkMessage, CancellationToken cancellationToken)
    {
        if (bookmarkMessage == null)
            return await _workflowEngine.ExecuteAsync(workflow, workflowState, cancellationToken);

        var bookmark =
            new Elsa.Models.Bookmark(
                bookmarkMessage.Id,
                bookmarkMessage.Name,
                bookmarkMessage.Hash,
                bookmarkMessage.Payload,
                bookmarkMessage.ActivityId,
                bookmarkMessage.ActivityInstanceId,
                bookmarkMessage.CallbackMethodName);

        return await _workflowEngine.ExecuteAsync(workflow, workflowState, bookmark, cancellationToken);
    }
}