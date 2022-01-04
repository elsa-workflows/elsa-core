using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
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

    public async Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var (definitionId, version) = request;
        var name = GetActorName(definitionId, version);

        var message = new DispatchWorkflowDefinition
        {
            Id = definitionId,
            Version = version
        };

        await _cluster.RequestAsync<Unit>(name, GrainKinds.WorkflowDefinition, message, cancellationToken);

        return new DispatchWorkflowResult();
    }

    public async Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var (definitionId, version) = request;
        var name = GetActorName(definitionId, version);

        var message = new ExecuteWorkflowDefinition
        {
            Id = definitionId,
            Version = version
        };

        var response = await _cluster.RequestAsync<ExecuteWorkflowResponse>(name, GrainKinds.WorkflowDefinition, message, cancellationToken);
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new ExecuteWorkflowResult(workflowState, bookmarks);
    }
        
    public async Task<DispatchWorkflowResult> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var (instanceId, bookmark) = request;
        var name = GetActorName(instanceId);
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new DispatchWorkflowInstance
        {
            Id = instanceId,
            Bookmark = bookmarkMessage
        };

        await _cluster.RequestAsync<Unit>(name, GrainKinds.WorkflowInstance, message, cancellationToken);

        return new DispatchWorkflowResult();
    }

    public async Task<ExecuteWorkflowResult> ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var (instanceId, bookmark) = request;
        var name = GetActorName(instanceId);
        var bookmarkMessage = MapBookmark(bookmark);

        var message = new ExecuteWorkflowInstance
        {
            Id = instanceId,
            Bookmark = bookmarkMessage
        };

        var response = await _cluster.RequestAsync<ExecuteWorkflowResponse>(name, GrainKinds.WorkflowInstance, message, cancellationToken);
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(response.WorkflowState.Text)!;
        var bookmarks = response.Bookmarks.Select(MapBookmark).ToList();

        return new ExecuteWorkflowResult(workflowState, bookmarks);
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
            ActivityId = bookmark.ActivityId,
            ActivityInstanceId = bookmark.ActivityInstanceId,
            CallbackMethodName = bookmark.CallbackMethodName,
            Data = { }
        };
    }

    private Bookmark MapBookmark(BookmarkMessage bookmarkMessage)
    {
        return new Bookmark(
            bookmarkMessage.Id,
            bookmarkMessage.Name,
            bookmarkMessage.Hash,
            bookmarkMessage.ActivityId,
            bookmarkMessage.ActivityInstanceId,
            bookmarkMessage.Data.ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<object>(x.Value.Text)),
            bookmarkMessage.CallbackMethodName
        );
    }

    private static string GetActorName(string definitionId, int version) => $"workflow-definition:{definitionId}:{version}";
    private static string GetActorName(string instanceId) => $"workflow-instance:{instanceId}";
}