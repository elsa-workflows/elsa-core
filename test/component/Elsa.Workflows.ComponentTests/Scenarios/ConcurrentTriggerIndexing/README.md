# Concurrent Trigger Indexing Tests

This test suite validates the fix for duplicate trigger registration in multi-engine environments.

GitHub Issue: [7130](https://github.com/elsa-workflows/elsa-core/issues/7130)

## Problem Description

In a multi-engine deployment where workflows are loaded dynamically from blob storage, HTTP endpoint triggers could be registered multiple times due to race conditions, leading to:
- Ambiguous routing errors: `"The call is ambiguous and matches multiple workflows"`
- Duplicate trigger entries in the database
- Unusable HTTP endpoints

## Solution

The fix implements three layers of protection:

1. **Distributed Locking** (`TriggerIndexer.cs`)
   - Ensures only one engine can index triggers for a specific workflow at a time
   - Lock key: `trigger-indexer:{workflowDefinitionId}`

2. **Database Unique Constraint** (`Configurations.cs`)
   - Unique index on `(WorkflowDefinitionId, Hash, ActivityId)`
   - Database-level protection against duplicates

3. **Retry Logic with Duplicate Detection** (`TriggerStore.cs`)
   - Catches duplicate key violations
   - Filters out existing triggers on retry
   - Gracefully handles edge cases

## Test Scenarios

### Parameterized test: `ConcurrentIndexing_ShouldNotCreateDuplicates`

Tests three scenarios using native TUnit arguments:
- **Synchronized start** (10 operations, no delay): All engines start simultaneously
- **Staggered start** (10 operations, 1-5ms random delays): Simulates real-world timing variations
- **Multiple rounds** (3 operations × 3 rounds): Tests repeated indexing operations

### Test: `ConcurrentWorkflowRefresh_ShouldNotCreateDuplicates`

Tests concurrent calls to the workflow refresh API from multiple engines, simulating:
- Manual API refresh requests
- File watcher triggered refreshes
- Simultaneous blob storage updates

### Test: `ManuallyCreatedDuplicates_ShouldHaveSameHash`

Documents the original bug symptom by deliberately creating duplicates to verify they have the same hash, which would cause ambiguous workflow matching.

## Test Architecture

### Helper Methods

- **`CreateAndSaveTestWorkflowAsync()`**: Creates a workflow with an HTTP endpoint trigger
- **`ExecuteConcurrentIndexingAsync()`**: Simulates concurrent trigger indexing with configurable parameters
- **`CreateIndexingTask()`**: Creates a single indexing task with optional delay
- **`GetPodByIndex()`**: Cycles through available pods (Pod1, Pod2, Pod3)
- **`AssertSingleTriggerExistsAsync()`**: Verifies no duplicate triggers exist

### Test Infrastructure

Uses the native TUnit component-test infrastructure:

- **Infrastructure**: Owns one SQL Server Testcontainer for the TUnit test session
- **App**: Owns the SQL catalog and filesystem root shared by the serialized component-test suite
- **Cluster**: Starts Pod1 for the session and creates Pod2 and Pod3 lazily when a scenario needs them
- **Host**: Boots the dedicated ASP.NET Core component-test entry point through the TUnit.AspNetCore lifecycle gate

## Running the Tests

```bash
# Run all concurrent trigger tests
dotnet test --project test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj -- --treenode-filter "/*/*/ConcurrentTriggerIndexingTests*/*"

# Run specific test
dotnet test --project test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj -- --treenode-filter "/*/*/ConcurrentTriggerIndexingTests*/ConcurrentIndexing_ShouldNotCreateDuplicates"
```

## Test Validation

These tests:
- **Failed before the fix**: Detecting 2-10 duplicate triggers
- **Pass after the fix**: Asserting exactly 1 trigger exists
- **Prevent regression**: Will fail if the race condition is reintroduced

## Related Files

- `src/modules/Elsa.Workflows.Runtime/Services/TriggerIndexer.cs`
- `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/Configurations.cs`
- `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/TriggerStore.cs`
