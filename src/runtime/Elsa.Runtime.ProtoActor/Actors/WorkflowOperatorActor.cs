using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Management.Serialization;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.ProtoActor.Extensions;
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
    private readonly IWorkflowRunner _workflowRunner;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public WorkflowOperatorActor(IRequestSender requestSender, IWorkflowRegistry workflowRegistry, IWorkflowRunner workflowRunner, WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _requestSender = requestSender;
        _workflowRegistry = workflowRegistry;
        _workflowRunner = workflowRunner;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
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
        var input = message.Input?.Deserialize();
        var executionResult = await ExecuteAsync(workflow, workflowState, bookmarkMessage, input, cancellationToken);
        var response = MapResult(executionResult);

        context.Respond(response);
    }

    private ExecuteWorkflowResponse MapResult(ExecuteWorkflowResult result)
    {
        var bookmarks = result.Bookmarks.Select(x => new Bookmark
        {
            Id = x.Id,
            Hash = x.Hash,
            Payload = x.Data,
            Name = x.Name,
            ActivityId = x.ActivityId,
            ActivityInstanceId = x.ActivityInstanceId,
            CallbackMethodName = x.CallbackMethodName
        });
        
        var options = _workflowSerializerOptionsProvider.CreatePersistenceOptions();

        var response = new ExecuteWorkflowResponse
        {
            WorkflowState = new Json
            {
                Text = JsonSerializer.Serialize(result.WorkflowState, options)
            }
        };

        response.Bookmarks.Add(bookmarks);

        return response;
    }

    private async Task<ExecuteWorkflowResult> ExecuteAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmarkMessage, IDictionary<string, object>? input, CancellationToken cancellationToken)
    {
        if (bookmarkMessage == null)
            return await _workflowRunner.RunAsync(workflow, workflowState, input, cancellationToken);

        var bookmark =
            new Elsa.Models.Bookmark(
                bookmarkMessage.Id,
                bookmarkMessage.Name,
                bookmarkMessage.Hash,
                bookmarkMessage.Payload,
                bookmarkMessage.ActivityId,
                bookmarkMessage.ActivityInstanceId,
                bookmarkMessage.CallbackMethodName);

        return await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }
}