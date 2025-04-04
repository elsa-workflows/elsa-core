# 3. Direct Bookmark Management in WorkflowExecutionContext

Date: 2025-04-01

## Status

Accepted

## Context
Currently, `ActivityExecutionContext` maintains a temporary list of bookmarks created by the activity during its execution. After the activity completes, a middleware is responsible for copying these bookmarks into the `WorkflowExecutionContext.Bookmarks` list, which is then persisted to the database.

This architecture leads to a subtle issue upon resumption:
- When a workflow is resumed from a persisted state, only the `WorkflowExecutionContext` and its `Bookmarks` are restored from the database.
- The `ActivityExecutionContext`'s temporary `Bookmarks` list remains empty.
- As a result, if the resumed activity attempts to cancel (or modify) its previously created bookmarks, it cannot, because it has no access to them—changes are not tracked.

However, there is an existing convention that determines that if an activity created a bookmark during its execution, the activity will not automatically complete.
Specifically, this is implemented in the `AutoCompleteBehavior`, which is installed by the `CodeActivity` and possibly by custom activities.
To keep this behavior in tact, we will still maintain a private list of bookmarks, one that is explicitly purposed for maintaining *new** bookmarks, temporarily for the lifetime of the ActivityExecutionContext in memory.  

## Decision
Eliminate the temporary `Bookmarks` list in `ActivityExecutionContext`. Instead, have activities add bookmarks directly to the `WorkflowExecutionContext.Bookmarks` list.

This is possible and safe because each `Bookmark` already contains:
- A reference to the originating `ActivityId`
- A reference to the originating `ActivityInstanceId`

These references allow accurate tracking and filtering of bookmarks, even when multiple activities are active.

Additionally, maintain a new temporary bookmarks list that explicitly stores newly created bookmarks. This list does not have to be copied into the WorkflowExecutionContext's Bookmarks property, given that this property will now be updated directly.

## Consequences

- Simplifies the architecture by removing the need for middleware to copy bookmarks post-execution.
- Fixes a bug where resumed activities could not cancel their previously created bookmarks.
- Reduces the chance of inconsistencies between temporary and persisted bookmark state.
- Ensures bookmark changes are always applied to the persisted, canonical source of truth.

## Alternatives Considered
- Keep the existing temporary list and enhance the system to hydrate the `ActivityExecutionContext.Bookmarks` list on resume. Rejected due to added complexity and duplication of state.
