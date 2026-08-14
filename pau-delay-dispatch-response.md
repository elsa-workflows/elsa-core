# Draft response to Pau

Hi Pau,

Thanks for the detailed information and screenshots. The confirmed Elsa version `3.6.1` is very helpful.

## TL;DR

- We started a focused `3.6.1` reproduction on our side.
- So far, seeing only `Elsa.Delay` in the active `Bookmarks` table after the parent has passed `Dispatch Workflow` looks expected.
- The current active blocker appears to be the Delay bookmark, so the next checks are Delay payload, Quartz trigger/job, logs, and workflow instance state.
- Please send the smallest Studio export that reproduces the issue.
- If possible, configure that reproduction with SQLite and include the SQLite database file captured after the workflow gets stuck.

We have also started a focused `3.6.1` reproduction on our side, using a small parent/child workflow with `Dispatch Workflow`, `Wait for Completion`, and a subsequent `Delay`. So far, the active bookmark table containing only `Elsa.Delay` after the parent continues past `Dispatch Workflow` looks expected. Once the dispatch bookmark has resumed the parent, it should be consumed and disappear from the active `Bookmarks` table.

At the same time, from what you sent, I do not yet see enough to reproduce your exact scenario end to end. The fastest route for us to investigate this properly, and to fix the full issue if it is a bug, would still be for you to send us a minimal reproduction exported from Elsa Studio: the smallest set of workflow definitions that still reproduces the issue.

Ideally this would include:

- The parent workflow exported from Studio.
- The child workflow exported from Studio.
- Only the minimum activities required to reproduce the problem.
- The exact configuration of `Dispatch Workflow` / `Wait for Completion`.
- The final `Delay` activity.
- Any variables/outputs involved in passing data from the child workflow back to the parent.
- The approximate reproduction rate, for example “1 in 10 executions” or “always when run under load”.
- If possible, a reproduction configured against SQLite, with the SQLite database file included after the issue occurs. That would let us inspect the workflow definitions, workflow instances, bookmarks, queue items, execution logs, and persisted state exactly as they are after the failure.

Without that, we can investigate the general mechanism and likely failure modes, but we may still miss a detail that only exists in your actual workflow structure, variable/output configuration, persistence setup, or Quartz configuration.

## What I think is happening

In Elsa `3.6.1`, `Dispatch Workflow` with `Wait for Completion` works roughly like this:

1. The parent dispatches the child workflow.
2. The parent creates a bookmark named `Elsa.DispatchWorkflow`.
3. When the child workflow finishes, Elsa enqueues a bookmark queue item for the parent.
4. The bookmark queue processor resumes the parent using the `Elsa.DispatchWorkflow` bookmark.
5. The parent continues execution.
6. If the parent later reaches a `Delay`, Elsa creates a new bookmark named `Elsa.Delay`.

Because of that, if the parent has already continued past `Dispatch Workflow` and then stopped at the `Delay`, it is expected that the active bookmark table contains only the `Elsa.Delay` bookmark. In that state, the `Elsa.DispatchWorkflow` bookmark should normally no longer be present in the active `Bookmarks` table because it has already been consumed.

So the database result showing only `Elsa.Delay` for the parent instance is not necessarily wrong. It can mean that `Wait for Completion` did resume correctly, and the current active blocker is the `Delay`.

The part I would like to verify is the workflow state view that still shows `Elsa.DispatchWorkflow`. If that is the latest persisted state for the same parent instance, then we may have an inconsistency between the serialized workflow state and the active bookmark index. If it is an older state snapshot, a cached UI view, or a different instance, then the database result may be the more accurate indicator of the current active bookmark.

## How to determine if the workflow is actually stuck

For a given parent workflow instance, please check these together:

1. `WorkflowInstances`: status/sub-status.
2. `Bookmarks`: active bookmarks for that workflow instance.
3. `BookmarkQueueItems`: pending resume requests for that workflow instance.
4. `WorkflowExecutionLogRecords`: last execution messages for that workflow instance.
5. `ActivityExecutionRecords`: latest activity records, especially `Elsa.DispatchWorkflow` and `Elsa.Delay`.
6. Quartz job/trigger tables: scheduled job for the active `Elsa.Delay` bookmark ID.

In the state JSON from your screenshot, `Status = 0` means `Running` and `SubStatus = 2` means `Suspended`. That is normal for a workflow waiting on a bookmark. A workflow is “stuck” only if it is suspended on a bookmark that should have been resumed already, for example a Delay whose `ResumeAt` is in the past and whose Quartz trigger has not fired or cannot resume the instance.

## What database tables to inspect

For one affected parent workflow instance ID, please inspect:

```sql
-- Active bookmarks for the parent instance.
select *
from [Elsa].[Bookmarks]
where [WorkflowInstanceId] = '<parent-workflow-instance-id>'
order by [CreatedAt] desc;

-- Pending bookmark queue items for the parent instance.
select *
from [Elsa].[BookmarkQueueItems]
where [WorkflowInstanceId] = '<parent-workflow-instance-id>'
order by [CreatedAt] desc;

-- Activity execution records around DispatchWorkflow and Delay.
select *
from [Elsa].[ActivityExecutionRecords]
where [WorkflowInstanceId] = '<parent-workflow-instance-id>'
order by [StartedAt] desc;

-- Workflow execution log.
select *
from [Elsa].[WorkflowExecutionLogRecords]
where [WorkflowInstanceId] = '<parent-workflow-instance-id>'
order by [Timestamp] desc, [Sequence] desc;
```

Also inspect the workflow instance row itself and confirm that the workflow state shown in the UI belongs to the same parent instance ID that you used in the bookmark query.

For Quartz, check whether there is a job/trigger for the active `Elsa.Delay` bookmark ID. The Delay bookmark ID should be the scheduler task/job key Elsa uses for that Delay.

## Logs to enable

For a short reproduction window, I suggest:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Elsa": "Debug",
      "Elsa.Workflows.Runtime": "Debug",
      "Elsa.Scheduling": "Debug",
      "Quartz": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

If the logs are too noisy, keep `Elsa.Workflows.Runtime`, `Elsa.Scheduling`, and `Quartz` at `Debug`, and keep the rest at `Information`.

The most useful log points are:

- Creation of the `Elsa.DispatchWorkflow` bookmark.
- Enqueueing of the bookmark queue item when the child workflow finishes.
- Processing of `BookmarkQueueItems`.
- Successful resume of the parent workflow.
- Creation of the `Elsa.Delay` bookmark.
- Scheduling of the Delay bookmark in Quartz.
- Quartz firing the Delay job.
- Resume attempt for the Delay bookmark.

## About a possible race condition

Yes, this area is timing-sensitive by nature: the child can finish before or around the time the parent has committed its `Elsa.DispatchWorkflow` bookmark.

However, Elsa `3.6.1` has a bookmark queue mechanism intended to handle exactly this kind of race. When the child finishes, Elsa does not require the parent bookmark to already exist at that exact millisecond. It enqueues a bookmark queue item, and the queue processor retries matching it against the bookmark store.

That means the first thing I would check is not only the `Bookmarks` table, but also `BookmarkQueueItems`. If old `Elsa.DispatchWorkflow` queue items remain there, the parent resume request may not be matching the bookmark. If there are no old dispatch queue items and the parent is currently waiting on `Elsa.Delay`, then the dispatch/wait part probably completed and the issue is more likely in Delay/Quartz resume.

## Could Delay be related?

Yes, but based on the screenshots it may be the current active blocker rather than the cause of the dispatch bookmark issue.

If the parent reaches `Delay`, creates an `Elsa.Delay` bookmark, and then never resumes, the key questions are:

- Does the `Elsa.Delay` bookmark payload contain a `ResumeAt` time that is already in the past?
- Does Quartz have a trigger for that exact bookmark ID?
- Did Quartz fire the trigger?
- Did Elsa receive/process the resume request?
- Did the resume fail because the instance was no longer `Running`, was faulted, or had a bookmark mismatch?

## What you can do to minimize impact

Until we have a reproduction, the practical mitigations are:

1. Monitor `BookmarkQueueItems` for old rows. Old rows can indicate resume requests that never matched a bookmark.
2. Monitor `Bookmarks` for `Elsa.Delay` rows whose resume time is in the past.
3. Monitor Quartz triggers for Delay jobs that should already have fired.
4. Add temporary diagnostic logging around Elsa runtime, scheduling, and Quartz.
5. Avoid relying on the 3-second Delay as a synchronization mechanism if possible; if output propagation needs a delay to work reliably, that is a separate symptom we should include in the reproduction.

## What I need from you next

Please send a minimal Studio export that reproduces the issue. If possible, include:

1. Parent workflow.
2. Child workflow.
3. Any required variables and outputs.
4. Steps to run it.
5. Expected behavior.
6. Actual behavior.
7. Preferably, a SQLite-backed reproduction database captured after the workflow gets stuck.
8. One affected parent workflow instance ID with matching rows from:
   - `Bookmarks`
   - `BookmarkQueueItems`
   - `WorkflowInstances`
   - `ActivityExecutionRecords`
   - `WorkflowExecutionLogRecords`
   - Quartz trigger/job tables

Once we can reproduce it locally, we can determine whether this is a configuration issue, a stale Quartz schedule, a bookmark queue issue, or a bug in Elsa `3.6.1`.

Best regards,

Sipke
