using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.UserTask.Bookmarks;
using Elsa.Activities.UserTask.Contracts;
using Elsa.Activities.UserTask.Models;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.UserTask.Services;

public class UserTaskService : IUserTaskService
{
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkSerializer _bookmarkSerializer;
    private readonly IWorkflowLaunchpad _workflowLaunchpad;

    public UserTaskService(IBookmarkStore bookmarkStore, IBookmarkSerializer bookmarkSerializer,IWorkflowLaunchpad workflowLaunchpad)
    {
        _bookmarkStore = bookmarkStore;
        _bookmarkSerializer = bookmarkSerializer;
        _workflowLaunchpad = workflowLaunchpad;
    }

    public async Task<IEnumerable<UserAction>> GetUserActionsAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var specification = BookmarkTypeAndWorkflowInstanceSpecification.For<UserTaskBookmark>(workflowInstanceId);
        var bookmarks = await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
        var userTaskBookmarks = bookmarks.Select(bookmark => _bookmarkSerializer.Deserialize<UserTaskBookmark>(bookmark.Model)).ToList();
        return userTaskBookmarks.Select(x => new UserAction(workflowInstanceId, x.Action));
    }

    public async Task<IEnumerable<UserAction>> GetAllUserActionsAsync(int? skip = default, int? take = default, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var specification = BookmarkTypeSpecification.For<UserTaskBookmark>(tenantId);
        var paging = Paging.Create(skip, take);
        var bookmarks = await _bookmarkStore.FindManyAsync(specification, paging: paging, cancellationToken: cancellationToken);
        
        return bookmarks.Select(bookmark =>
        {
            var model = _bookmarkSerializer.Deserialize<UserTaskBookmark>(bookmark.Model);
            return new UserAction(bookmark.WorkflowInstanceId, model.Action);
        });
    }

    public async Task<IEnumerable<CollectedWorkflow>> ExecuteUserActionsAsync(TriggerUserAction taskAction, CancellationToken cancellationToken = default)
    {
        var bookmark = new UserTaskBookmark(taskAction.Action);
        var query = new WorkflowsQuery(nameof(Activities.UserTask), bookmark, taskAction.CorrelationId, taskAction.WorkflowInstanceId);
        return await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(query,new WorkflowInput(taskAction.Action),cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<CollectedWorkflow>> DispatchUserActionsAsync(TriggerUserAction taskAction, CancellationToken cancellationToken = default)
    {
        var bookmark = new UserTaskBookmark(taskAction.Action);
        var query = new WorkflowsQuery(nameof(Activities.UserTask), bookmark, taskAction.CorrelationId, taskAction.WorkflowInstanceId);
        return await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(query,new WorkflowInput(taskAction.Action),cancellationToken:cancellationToken);
    }
    

    
}