using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Proto;
using Proto.Cluster;
using Bookmark = Elsa.Workflows.Core.Models.Bookmark;
using ProtoBookmark = Elsa.Runtime.Protos.Bookmark;

namespace Elsa.ProtoActor.Implementations;

public class ProtoActorWorkflowInvoker : IWorkflowInvoker
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly Cluster _cluster;
    private readonly GrainClientFactory _grainClientFactory;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    public ProtoActorWorkflowInvoker(
        IWorkflowDefinitionService workflowDefinitionService,
        Cluster cluster, 
        GrainClientFactory grainClientFactory, 
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _cluster = cluster;
        _grainClientFactory = grainClientFactory;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var (definitionId, versionOptions, input, correlationId) = request;

        var message = new ExecuteWorkflowDefinitionRequest
        {
            Id = definitionId,
            VersionOptions = versionOptions.ToString(),
            Input = input!?.Serialize(),
            CorrelationId = correlationId ?? ""
        };

        var client = _grainClientFactory.CreateWorkflowDefinitionGrainClient(definitionId, versionOptions);
        var response = await client.Execute(message, CancellationTokens.FromSeconds(10000));

        if (response == null)
            throw new TimeoutException("Did not receive a response from the WorkflowDefinition actor within the configured amount of time.");

        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new InvokeWorkflowResult(workflowState, bookmarks);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var (instanceId, bookmark, input, correlationId) = request;
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new ExecuteExistingWorkflowInstanceRequest
        {
            InstanceId = instanceId,
            Bookmark = bookmarkMessage,
            Input = input!?.Serialize(),
        };

        var client = _grainClientFactory.CreateWorkflowInstanceGrainClient(instanceId);
        var response = await client.ExecuteExistingInstance(message, CancellationTokens.FromSeconds(600));

        if (response == null)
            throw new TimeoutException("Did not receive a response from the WorkflowInstance actor within the configured amount of time.");

        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new InvokeWorkflowResult(workflowState, bookmarks);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new ExecuteExistingWorkflowInstanceRequest
        {
            Bookmark = bookmarkMessage,
            Input = input!?.Serialize(),
            InstanceId = workflowInstance.Id
        };

        var client = _grainClientFactory.CreateWorkflowInstanceGrainClient(workflowInstance.Id);
        var response = await client.ExecuteExistingInstance(message, CancellationTokens.FromSeconds(600));

        if (response == null)
            throw new TimeoutException("Did not receive a response from the WorkflowInstance actor within the configured amount of time.");

        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text, _workflowSerializerOptionsProvider.CreateDefaultOptions())!;
        return new InvokeWorkflowResult(workflowState, bookmarks);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(
        WorkflowDefinition workflowDefinition, 
        WorkflowState workflowState, 
        Bookmark? bookmark = default, 
        IDictionary<string, object>? input = default, 
        CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        return await InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new ExecuteWorkflowRequest
        {
            Bookmark = bookmarkMessage,
            Input = input!?.Serialize(),
            DefinitionId = workflow.Identity.DefinitionId,
            VersionOptions = VersionOptions.SpecificVersion(workflow.Identity.Version).ToString(),
            WorkflowState = JsonSerializer.Serialize(workflowState, _workflowSerializerOptionsProvider.CreateDefaultOptions())
        };

        var client = _grainClientFactory.CreateWorkflowInstanceGrainClient(workflowState.Id);
        var response = await client.Execute(message, CancellationTokens.FromSeconds(600));

        if (response == null)
            throw new TimeoutException("Did not receive a response from the WorkflowInstance actor within the configured amount of time.");

        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new InvokeWorkflowResult(workflowState, bookmarks);
    }

    private static ProtoBookmark? MapBookmark(Bookmark? bookmark)
    {
        if (bookmark == null)
            return null;

        return new ProtoBookmark
        {
            Id = bookmark.Id,
            Name = bookmark.Name,
            Hash = bookmark.Hash,
            Payload = bookmark.Data,
            ActivityId = bookmark.ActivityId,
            ActivityInstanceId = bookmark.ActivityInstanceId,
            CallbackMethodName = bookmark.CallbackMethodName,
        };
    }

    private static Bookmark MapBookmark(ProtoBookmark protoBookmark)
    {
        return new Bookmark(
            protoBookmark.Id,
            protoBookmark.Name,
            protoBookmark.Hash,
            protoBookmark.Payload,
            protoBookmark.ActivityId,
            protoBookmark.ActivityInstanceId,
            protoBookmark.CallbackMethodName
        );
    }
}