# Reply to Jordi - Delay bookmarks and manual recovery

Good morning Jordi,

Thanks for the detailed explanation.

Before we go too deep into the recovery options, can you please confirm the exact Elsa version you are running? You can usually check this with:

```http
GET /elsa/api/package/version
```

Alternatively, please send us the Elsa NuGet package versions used by the application.

## Short answer

The endpoint you are currently calling is not the right one for a Delay activity bookmark:

```http
POST /elsa/api/events/{eventName}/trigger
```

That endpoint is for Elsa `Event` activities. It publishes an event stimulus. A `200 OK` response means that the event request was accepted, but it does not mean that a Delay bookmark was matched or resumed.

For a `Delay` activity, Elsa stores a bookmark named:

```text
Elsa.Delay
```

The bookmark is matched by bookmark id, workflow instance id, activity instance id, and/or the generated bookmark hash. It is not resumed by using the designer activity name such as `Delay8` or `Delay.8`.

Normally, Quartz should resume this Delay automatically. Quartz schedules a task that resumes the workflow instance using the stored `WorkflowInstanceId` and `BookmarkId`.

## Recommended first step: reproduce or inspect the stuck case

The best next step is to determine why Quartz did not resume the Delay bookmark. If we can reproduce the issue, we can fix the root cause instead of only providing a manual workaround.

For one stuck instance, please capture:

- Elsa version.
- Workflow instance id.
- Bookmark id.
- Activity instance id of the Delay activity.
- The `Elsa.Bookmarks` row for that workflow instance.
- The matching Quartz job/trigger row.
- Logs around the expected Delay resume time.
- Whether the workflow instance is still `Running`, or whether it was already cancelled/faulted/finished.

For example, for a stuck Delay bookmark, the relevant values would look like:

```text
BookmarkId:          <bookmark-id>
WorkflowInstanceId:  <workflow-instance-id>
ActivityInstanceId:  <delay-activity-instance-id>
Bookmark name:       Elsa.Delay
```

## How to manually resume a stuck Delay safely

I would not recommend directly updating workflow instance state in the database. Resuming a workflow is not just a database state change. The Elsa runtime has to:

- Load the workflow instance.
- Resume the correct bookmarked activity.
- Remove the consumed bookmark.
- Persist the new workflow state.
- Save variables.
- Write execution logs.
- Emit the notifications that scheduling cleanup depends on.

The safest manual recovery option is to expose a temporary authenticated admin endpoint in your Elsa server that calls Elsa's runtime service `IWorkflowResumer`.

Example:

```csharp
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("admin/elsa/bookmarks")]
[Authorize] // Restrict this further in production.
public class ElsaBookmarkAdminController : ControllerBase
{
    [HttpPost("{bookmarkId}/resume")]
    public async Task<IActionResult> Resume(
        string bookmarkId,
        [FromBody] ResumeBookmarkAdminRequest request,
        [FromServices] IWorkflowResumer workflowResumer,
        CancellationToken cancellationToken)
    {
        var responses = await workflowResumer.ResumeAsync(new ResumeBookmarkRequest
        {
            BookmarkId = bookmarkId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);

        var responseList = responses.ToList();

        if (responseList.Count == 0)
            return NotFound(new
            {
                message = "No matching bookmark was found or resumed.",
                bookmarkId,
                request.WorkflowInstanceId,
                request.ActivityInstanceId
            });

        return Ok(new
        {
            resumed = responseList.Count,
            responses = responseList
        });
    }
}

public class ResumeBookmarkAdminRequest
{
    public string WorkflowInstanceId { get; set; } = default!;
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
}
```

Example request:

```http
POST /admin/elsa/bookmarks/2cdf1537300ebbde/resume
Content-Type: application/json

{
  "workflowInstanceId": "2f95829eb7abe851",
  "activityInstanceId": "2aacb800a50923f4"
}
```

If the bookmark is still valid and the workflow instance is still running, this should resume the Delay through the Elsa runtime.

## Why not use `/events/{eventName}/trigger`?

This endpoint resumes workflows waiting on an Elsa `Event` activity. Internally, it publishes an event stimulus.

A Delay bookmark uses the scheduling stimulus name:

```text
Elsa.Delay
```

and its payload contains the `resumeAt` timestamp. So calling:

```http
POST /elsa/api/events/Delay8/trigger
```

or:

```http
POST /elsa/api/events/Delay.8/trigger
```

will not match a Delay bookmark.

## Built-in bookmark resume endpoint

Elsa also has a built-in bookmark resume endpoint:

```http
POST /elsa/api/bookmarks/resume?t={token}
```

However, this endpoint expects a signed bookmark token, not just the bookmark id from the database. It is useful when a workflow generated a bookmark resume URL/token, but it is usually not the practical recovery path for an internal Delay activity that was scheduled by Quartz.

For this scenario, the small admin endpoint above is more appropriate.

## If you still want a database-level fallback

If you absolutely need a database-level operation, I would avoid editing these directly:

- `Elsa.WorkflowInstances`
- `Elsa.Bookmarks`
- Quartz trigger state, unless we are only cleaning up after a cancelled/deleted instance

Changing those manually can leave the workflow state, runtime bookmarks, variables, logs, and scheduler state inconsistent.

The least invasive database-style nudge is to insert a bookmark queue item and let Elsa's bookmark queue processor resume it. This still requires the Elsa application to be running and processing bookmark queue items.

Important: the exact table and column names can vary by Elsa version and provider, so please confirm your version before using SQL like this.

Illustrative SQL Server example:

```sql
INSERT INTO Elsa.BookmarkQueueItems
(
    Id,
    WorkflowInstanceId,
    BookmarkId,
    ActivityInstanceId,
    ActivityTypeName,
    CreatedAt,
    SerializedOptions,
    TenantId
)
VALUES
(
    LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
    '2f95829eb7abe851',
    '2cdf1537300ebbde',
    '2aacb800a50923f4',
    'Elsa.Delay',
    SYSUTCDATETIME(),
    NULL,
    NULL
);
```

Notes:

- This does not directly resume the workflow.
- It only creates work for Elsa's bookmark queue processor.
- If the queue processor is not running, nothing will happen.
- A direct DB insert bypasses Elsa's in-process queue signaler, so you may need to wait for the recurring queue worker or restart the Elsa application.
- This is still less preferable than calling an application endpoint that invokes `IWorkflowResumer` or `IBookmarkQueue`.

If you want a slightly safer variant of this queue approach, expose an admin endpoint that enqueues the bookmark through Elsa instead of inserting into the database manually:

```csharp
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("admin/elsa/bookmark-queue")]
[Authorize]
public class ElsaBookmarkQueueAdminController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Enqueue(
        [FromBody] EnqueueBookmarkRequest request,
        [FromServices] IBookmarkQueue bookmarkQueue,
        CancellationToken cancellationToken)
    {
        await bookmarkQueue.EnqueueAsync(new NewBookmarkQueueItem
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            BookmarkId = request.BookmarkId,
            ActivityInstanceId = request.ActivityInstanceId,
            ActivityTypeName = "Elsa.Delay"
        }, cancellationToken);

        return Accepted();
    }
}

public class EnqueueBookmarkRequest
{
    public string WorkflowInstanceId { get; set; } = default!;
    public string BookmarkId { get; set; } = default!;
    public string? ActivityInstanceId { get; set; }
}
```

That endpoint has the advantage that it uses Elsa's queue abstraction and triggers the queue processor.

## About cancelled instances and Quartz rows

If a workflow instance is cancelled, it should not resume anymore. Any remaining Delay bookmark or Quartz job for that instance is stale data.

In the normal flow, when bookmarks are deleted through Elsa, the scheduling layer is notified and the corresponding scheduled jobs are unscheduled. However, whether cancellation also removes the persisted bookmark and Quartz job in your exact setup depends on the Elsa version and the cancellation path being used.

That is why I would like to verify your version before giving a definitive answer here.

If cancelled instances still leave `Elsa.Delay` bookmarks or Quartz jobs behind, please send one example and we can check whether:

- this is already fixed in a newer Elsa version;
- the cancellation path is bypassing the bookmark cleanup flow;
- the Quartz job is stale and can be cleaned safely;
- or we need to add a cleanup/fix.

## About the 3-second Delay workaround

The 3-second Delay before the child workflow finishes sounds like a workaround for a race/timing issue in how values are propagated back to the parent workflow.

Variable propagation from child to parent should not depend on sleeping. Once I know your Elsa version and can see a minimal reproduction, I can check whether this is a known issue, a fixed issue, or whether there is a better pattern for returning values from the child workflow to the parent.

## Suggested recovery order

For the current stuck instances, I suggest this order:

1. Confirm the Elsa version with `GET /elsa/api/package/version`.
2. Pick one stuck workflow instance as an example.
3. Verify the workflow instance is still `Running`.
4. Verify the `Elsa.Bookmarks` row exists and has `Name = 'Elsa.Delay'`.
5. Verify the Quartz job/trigger exists for the same bookmark id.
6. Check logs around the expected resume time.
7. Try manual resume through the temporary `IWorkflowResumer` admin endpoint.
8. If that works, we know the workflow itself can continue and the problem is likely in scheduling/Quartz execution.
9. If it does not work, capture the response/logs because the bookmark may no longer match the workflow state, or the instance may no longer be resumable.
10. Only use database queue insertion as a last resort, and only after confirming the exact Elsa version/schema.

Best regards,

Sipke
