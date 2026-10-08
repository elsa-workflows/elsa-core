# 4. Activity Execution Snapshots

**Date:** 2025-08-04  
**Status:** Accepted  

## Context

Today the `ActivityExecutionContext` is persisted only at *commit points*.  
If an activity references a workflow variable that changes after it has run—but before the next commit—the value saved is the *later* value, not the one that existed when the activity executed.  
As a result, the Workflow Instance Viewer shows misleading data: users expect to see the variable values *at execution time*, not at commit time.

## Decision

Capture a **snapshot** of the `ActivityExecutionContext` immediately when an activity executes.  
The snapshot must include:

* All workflow variables and their values at that moment.  
* Any other execution-specific metadata required for replay or inspection.

The snapshot is created by serializing the `ActivityExecutionRecord` to JSON and storing it in the database.
The persistence layer will be updated to handle this new snapshot field, ensuring it is stored alongside the activity execution record.

## Consequences

* The Workflow Instance Viewer will now display the exact state that the activity saw, eliminating confusion during debugging and auditing.  
* Additional storage will be consumed for each snapshot. We accept this overhead in exchange for correctness and developer experience.  
* Existing persistence schemas will require a non-breaking migration to store the snapshot payload.
* Workflow instances before this change will not have snapshots, but they will still be replayable even if the variable values are not accurate at execution time.