using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.Protos;
using Elsa.Workflows.Runtime.Services;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.State;
using Proto;
using Bookmark = Elsa.Workflows.Runtime.Protos.Bookmark;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Executes a workflow instance.
/// </summary>
public class WorkflowInstanceGrain : WorkflowInstanceGrainBase
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public WorkflowInstanceGrain(
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRunner workflowRunner,
        IWorkflowInstanceFactory workflowInstanceFactory,
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider,
        IContext context) : base(context)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowRunner = workflowRunner;
        _workflowInstanceFactory = workflowInstanceFactory;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    public override async Task<ExecuteWorkflowInstanceResponse> ExecuteExistingInstance(ExecuteExistingWorkflowInstanceRequest request)
    {
        var workflowInstanceId = request.InstanceId;
        var cancellationToken = Context.CancellationToken;
        var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

        if (workflowInstance == null)
            throw new Exception($"No workflow instance found with ID {workflowInstanceId}");

        var workflowDefinitionId = workflowInstance.DefinitionId;
        var workflow = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
            throw new Exception($"No workflow definition found with ID {workflowDefinitionId}");

        var workflowState = workflowInstance.WorkflowState;
        var bookmarkMessage = request.Bookmark;
        var input = request.Input?.Deserialize();
        var executionResult = await ExecuteAsync(workflow, workflowState, bookmarkMessage, input!, cancellationToken);
        var response = MapResult(executionResult);

        return response;
    }

    public override async Task<ExecuteWorkflowInstanceResponse> ExecuteNewInstance(ExecuteNewWorkflowInstanceRequest request)
    {
        var cancellationToken = Context.CancellationToken;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var workflowDefinitionId = request.DefinitionId;
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var workflowInstance = await _workflowInstanceFactory.CreateAsync(workflowDefinitionId, versionOptions, correlationId, cancellationToken);
        var workflow = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
            throw new Exception($"No workflow definition found with ID {workflowDefinitionId}");

        var workflowState = workflowInstance.WorkflowState;
        var input = request.Input?.Deserialize();
        var executionResult = await ExecuteAsync(workflow, workflowState, input: input!, cancellationToken: cancellationToken);
        var response = MapResult(executionResult);

        return response;
    }

    public override async Task<ExecuteWorkflowInstanceResponse> Execute(ExecuteWorkflowRequest request)
    {
        var cancellationToken = Context.CancellationToken;
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(request.WorkflowState, _workflowSerializerOptionsProvider.CreatePersistenceOptions())!;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var workflowDefinitionId = request.DefinitionId;
        var bookmark = request.Bookmark;
        var input = request.Input?.Deserialize();
        var workflow = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, versionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception($"No workflow definition found with ID {workflowDefinitionId}");

        var result = await ExecuteAsync(workflow, workflowState, bookmark, input, cancellationToken);
        return MapResult(result);
    }

    private ExecuteWorkflowInstanceResponse MapResult(InvokeWorkflowResult result)
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

        var response = new ExecuteWorkflowInstanceResponse
        {
            WorkflowState = new Json
            {
                Text = JsonSerializer.Serialize(result.WorkflowState, options)
            }
        };

        response.Bookmarks.Add(bookmarks);

        return response;
    }

    private async Task<InvokeWorkflowResult> ExecuteAsync(
        WorkflowDefinition workflowDefinition,
        WorkflowState workflowState,
        Bookmark? bookmarkMessage = default,
        IDictionary<string, object>? input = default,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        return await ExecuteAsync(workflow, workflowState, bookmarkMessage, input, cancellationToken);
    }

    private async Task<InvokeWorkflowResult> ExecuteAsync(
        Workflow workflow,
        WorkflowState workflowState,
        Bookmark? bookmarkMessage = default,
        IDictionary<string, object>? input = default,
        CancellationToken cancellationToken = default)
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