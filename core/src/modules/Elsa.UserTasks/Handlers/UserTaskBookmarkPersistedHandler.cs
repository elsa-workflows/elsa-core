using System.Text.Json;
using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.UserTasks.Handlers;

/// <summary>
/// Bridges the workflow bookmark commit notification to the User Tasks projection. This keeps workflow
/// and task persistence decoupled: task projection is never written from inside activity execution.
/// </summary>
public sealed class UserTaskBookmarkPersistedHandler(IUserTaskProjectionService projectionService, ISystemClock clock) : INotificationHandler<WorkflowBookmarksPersisted>
{
    public async Task HandleAsync(WorkflowBookmarksPersisted notification, CancellationToken cancellationToken)
    {
        var materializations = notification.Diff.Added
            .Select(Deserialize)
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();
        if (materializations.Length > 0)
            await projectionService.ProjectCommittedBookmarksAsync(materializations, cancellationToken);

        foreach (var bookmark in notification.Diff.Removed)
        {
            var materialization = Deserialize(bookmark);
            if (materialization?.TaskId == null)
                continue;
            await projectionService.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval(
                materialization.TenantId,
                materialization.TaskId,
                bookmark.Id,
                clock.UtcNow), cancellationToken);
        }
    }

    private static UserTaskMaterialization? Deserialize(Bookmark bookmark)
    {
        try
        {
            return bookmark.Payload switch
            {
                UserTaskMaterialization materialization => materialization,
                JsonElement element => element.Deserialize<UserTaskMaterialization>(),
                null => null,
                _ => JsonSerializer.Deserialize<UserTaskMaterialization>(JsonSerializer.Serialize(bookmark.Payload))
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
