using System.Text.Json;
using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Helpers;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.UserTasks.Handlers;

/// <summary>
/// Bridges the workflow bookmark commit notification to the User Tasks projection. This keeps workflow
/// and task persistence decoupled: task projection is never written from inside activity execution.
/// </summary>
public sealed class UserTaskBookmarkPersistedHandler(
    IUserTaskProjectionService projectionService,
    IUserTaskRepository repository,
    ISystemClock clock) : INotificationHandler<WorkflowBookmarksPersisted>
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
            if (bookmark.Name != nameof(Activities.UserTask))
                continue;

            var materialization = Deserialize(bookmark);
            var tenantId = materialization?.TenantId;
            var taskId = materialization?.TaskId;

            if (taskId == null)
            {
                tenantId ??= TryReadTenantId(bookmark.Payload) ?? "";
                var task = await repository.FindByBookmarkIdAsync(tenantId, bookmark.Id, cancellationToken);
                if (task == null)
                    continue;

                tenantId = task.TenantId;
                taskId = task.Id;
            }

            await projectionService.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval(
                tenantId ?? "",
                taskId,
                bookmark.Id,
                clock.UtcNow), cancellationToken);
        }
    }

    private static UserTaskMaterialization? Deserialize(Bookmark bookmark)
    {
        // Other activities (Cron, Delay, Event, ...) also persist bookmarks; only UserTask bookmarks carry a materialization.
        if (bookmark.Name != nameof(Activities.UserTask))
        {
            return null;
        }

        return UserTaskBookmarkPayload.ReadMaterialization(bookmark.Payload);
    }

    private static string? TryReadTenantId(object? payload) => payload switch
    {
        JsonElement element when element.ValueKind == JsonValueKind.Object && TryGetProperty(element, "tenantId", out var tenant) => tenant.GetString(),
        IDictionary<string, object?> bag when TryGetIgnoreCase(bag, "tenantId", out var value) => value?.ToString(),
        IDictionary<string, object> bag when TryGetIgnoreCase(bag, "tenantId", out var value) => value?.ToString(),
        _ => null
    };

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetIgnoreCase<T>(IDictionary<string, T> bag, string name, out T? value)
    {
        if (bag.TryGetValue(name, out value))
            return true;

        foreach (var (key, candidate) in bag)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }
}
