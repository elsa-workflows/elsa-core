using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.UserTasks.Services;

/// <summary>
/// Performs a bounded, tenant-scoped repair pass. The workflow bookmark is the source of truth: when a
/// bookmark exists, accepted transitions are retried; when it is gone, the projection is finalized after
/// commit. The bookmark store is optional so the service remains usable with the Core in-memory stack.
/// </summary>
public sealed class DefaultUserTaskReconciler(
    IUserTaskRepository repository,
    IUserTaskManager manager,
    IUserTaskProjectionService projectionService,
    IUserTaskWorkflowResumer workflowResumer,
    ISystemClock clock,
    IBookmarkStore? bookmarkStore = null) : IUserTaskReconciler
{
    public async Task<UserTaskReconciliationResult> ReconcileAsync(UserTaskReconciliationRequest request, CancellationToken cancellationToken = default)
    {
        var olderThan = request.OlderThan ?? clock.UtcNow.Subtract(TimeSpan.FromMinutes(5));
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var recreated = bookmarkStore == null ? 0 : await RecreateMissingProjectionsAsync(request.TenantId, pageSize, cancellationToken);
        var cursor = (string?)null;
        var requeued = 0;
        var finalized = 0;
        var ambiguous = 0;

        do
        {
            var result = await repository.QueryAsync(new UserTaskQuery
            {
                TenantId = request.TenantId,
                Cursor = cursor,
                Sort = "created",
                Limit = pageSize
            }, cancellationToken);
            foreach (var task in result.Items.Where(x => x.UpdatedAt <= olderThan))
            {
                var bookmark = bookmarkStore == null
                    ? null
                    : await bookmarkStore.FindAsync(new BookmarkFilter
                    {
                        BookmarkId = task.BookmarkId,
                        WorkflowInstanceId = task.WorkflowInstanceId
                    }, cancellationToken);

                if (bookmarkStore != null && bookmark == null && task.IsOpen)
                {
                    var before = task.Status;
                    await projectionService.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval(task.TenantId, task.Id, task.BookmarkId, clock.UtcNow), cancellationToken);
                    var after = await repository.GetAsync(task.TenantId, task.Id, cancellationToken);
                    if (after?.Status != before)
                        finalized++;
                    continue;
                }

                if (task.Status is not (UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling))
                    continue;

                var operation = task.Operations.LastOrDefault(x => x.Status == UserTaskOperationStatus.Accepted && x.Kind is (UserTaskOperationKind.Complete or UserTaskOperationKind.Timeout or UserTaskOperationKind.Cancel));
                if (operation == null)
                {
                    ambiguous++;
                    continue;
                }

                if (bookmarkStore != null && bookmark == null)
                {
                    var before = task.Status;
                    await projectionService.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval(task.TenantId, task.Id, task.BookmarkId, clock.UtcNow), cancellationToken);
                    var after = await repository.GetAsync(task.TenantId, task.Id, cancellationToken);
                    if (after?.Status != before)
                        finalized++;
                    continue;
                }

                try
                {
                    var action = task.Status == UserTaskStatus.TimingOut ? "Timeout" : task.Status == UserTaskStatus.Cancelling ? "Cancelled" : task.CompletionActionKey ?? "Complete";
                    await workflowResumer.ResumeAsync(task, new UserTaskStimulus(task.TenantId, task.Id, operation.OperationId,
                        action, task.CompletionData, task.CompletedBy, task.CompletedAt ?? operation.CreatedAt, task.BookmarkId), cancellationToken);
                    requeued++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Keep the accepted operation durable for the next bounded pass.
                    await repository.TryMutateAsync(task.TenantId, task.Id, task.Revision, current =>
                    {
                        current.HealthSeverity = UserTaskHealthSeverity.Advisory;
                        current.HealthCode = "stale-transition";
                        current.HealthMessage = "A workflow outcome requires reconciliation.";
                        return true;
                    }, cancellationToken);
                }
            }

            cursor = result.NextCursor;
        }
        while (cursor != null);

        return new UserTaskReconciliationResult(recreated, requeued, finalized, ambiguous);
    }

    private async Task<int> RecreateMissingProjectionsAsync(string tenantId, int pageSize, CancellationToken cancellationToken)
    {
        var recreated = 0;
        var page = 0;
        while (true)
        {
            var bookmarks = (await bookmarkStore!.FindManyAsync(new BookmarkFilter { Name = nameof(Elsa.UserTasks.Activities.UserTask) },
                PageArgs.FromPage(page, pageSize), cancellationToken)).Items.ToArray();
            if (bookmarks.Length == 0)
                break;

            foreach (var bookmark in bookmarks)
            {
                var materialization = Deserialize(bookmark);
                if (materialization == null || !string.Equals(materialization.TenantId, tenantId, StringComparison.Ordinal))
                    continue;
                if (await repository.FindByMaterializationKeyAsync(tenantId, MaterializationKey(materialization), cancellationToken) != null)
                    continue;
                await manager.ProjectAsync(materialization, cancellationToken);
                recreated++;
            }

            if (bookmarks.Length < pageSize)
                break;
            page++;
        }
        return recreated;
    }

    private static string MaterializationKey(UserTaskMaterialization materialization) => string.Join("/", materialization.TenantId, materialization.WorkflowInstanceId, materialization.ActivityInstanceId);

    private static UserTaskMaterialization? Deserialize(StoredBookmark bookmark)
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
