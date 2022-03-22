using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Runtime.ProtoActor.Extensions;
using Elsa.Runtime.ProtoActor.Messages;
using Elsa.State;
using Proto.Cluster;

namespace Elsa.Runtime.ProtoActor.Services;

using Bookmark = Elsa.Models.Bookmark;
using BookmarkMessage = Messages.Bookmark;

public class ProtoActorWorkflowInvoker : IWorkflowInvoker
{
    private readonly Cluster _cluster;

    public ProtoActorWorkflowInvoker(Cluster cluster)
    {
        _cluster = cluster;
    }

    public async Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var (definitionId, versionOptions, input) = request;
        var name = GetActorName(definitionId, versionOptions);

        var message = new ExecuteWorkflowDefinition
        {
            Id = definitionId,
            VersionOptions = versionOptions.ToString(),
            Input = input!?.Serialize()
        };

        var response = await _cluster.RequestAsync<ExecuteWorkflowResponse>(name, GrainKinds.WorkflowDefinition, message, cancellationToken);
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new ExecuteWorkflowResult(workflowState, bookmarks);
    }

    public async Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var (instanceId, bookmark, input) = request;
        var name = GetActorName(instanceId);
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new ExecuteWorkflowInstance
        {
            Id = instanceId,
            Bookmark = bookmarkMessage,
            Input = input?.Serialize()
        };

        var response = await _cluster.RequestAsync<ExecuteWorkflowResponse>(name, GrainKinds.WorkflowInstance, message, cancellationToken);
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new ExecuteWorkflowResult(workflowState, bookmarks);
    }

    public async Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var (definitionId, versionOptions, input) = request;
        var name = GetActorName(definitionId, versionOptions);

        var message = new DispatchWorkflowDefinition
        {
            Id = definitionId,
            VersionOptions = versionOptions.ToString(),
            Input = input!?.Serialize()
        };

        await _cluster.RequestAsync<Unit>(name, GrainKinds.WorkflowDefinition, message, cancellationToken);

        return new DispatchWorkflowResult();
    }

    public async Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var (instanceId, bookmark, input) = request;
        var name = GetActorName(instanceId);
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new DispatchWorkflowInstance
        {
            Id = instanceId,
            Bookmark = bookmarkMessage,
            Input = input!?.Serialize()
        };

        await _cluster.RequestAsync<Unit>(name, GrainKinds.WorkflowInstance, message, cancellationToken);

        return new DispatchWorkflowResult();
    }

    private BookmarkMessage? MapBookmark(Bookmark? bookmark)
    {
        if (bookmark == null)
            return null;

        return new BookmarkMessage
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

    private Bookmark MapBookmark(BookmarkMessage bookmarkMessage)
    {
        return new Bookmark(
            bookmarkMessage.Id,
            bookmarkMessage.Name,
            bookmarkMessage.Hash,
            bookmarkMessage.Payload,
            bookmarkMessage.ActivityId,
            bookmarkMessage.ActivityInstanceId,
            bookmarkMessage.CallbackMethodName
        );
    }

    private static string GetActorName(string definitionId, VersionOptions versionOptions) => $"workflow-definition:{definitionId}:{versionOptions.ToString()}";
    private static string GetActorName(string instanceId) => $"workflow-instance:{instanceId}";
}