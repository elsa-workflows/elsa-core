using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.UserTask.Bookmarks;
using Elsa.Activities.UserTask.Contracts;
using Elsa.Activities.UserTask.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Services;

namespace Elsa.Activities.UserTask.Services;

public class UserTaskService : IUserTaskService
{
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkSerializer _bookmarkSerializer;

    public UserTaskService(IBookmarkStore bookmarkStore, IBookmarkSerializer bookmarkSerializer)
    {
        _bookmarkStore = bookmarkStore;
        _bookmarkSerializer = bookmarkSerializer;
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
}