# Tool Catalog Contract: Weaver Grounding Tools

All tools use Elsa-owned `AIToolDefinition` metadata and execute server-side. Names are stable and namespaced.

## Activity Tools

### `activities.search`

**Mutability**: `ReadOnly`
**Purpose**: Find installed activities by capability, type, category, input/output, trigger behavior, or text query.

**Arguments**

```json
{
  "query": "http request",
  "category": "HTTP",
  "canStartWorkflow": true,
  "inputName": "Path",
  "outputName": "Body",
  "skip": 0,
  "take": 20
}
```

**Result**: `GroundingToolResult<ActivityGroundingSummary>`.

### `activities.getDescriptor`

**Mutability**: `ReadOnly`
**Purpose**: Return detailed model-safe metadata for one installed activity.

**Arguments**

```json
{
  "typeName": "Elsa.Http.Endpoint",
  "version": 1
}
```

**Result**: `ActivityGroundingSummary`.

## Workflow Definition Tools

### `workflows.search`

**Mutability**: `ReadOnly`
**Purpose**: Find authorized workflow definitions by name, status, activity usage, tag, or text query.

### `workflows.getDefinition`

**Mutability**: `ReadOnly`
**Purpose**: Return an authorized workflow definition summary and selected graph details.

### `workflows.getDefinitionGraph`

**Mutability**: `ReadOnly`
**Purpose**: Return graph-oriented activity and connection data for explanation, comparison, or proposal baselines.

### `workflows.findUsages`

**Mutability**: `ReadOnly`
**Purpose**: Find workflows that use an activity type, variable name, input, output, or expression syntax.

## Proposal Tools

### `workflows.validateDraft`

**Mutability**: `Proposal`
**Purpose**: Validate a draft workflow payload without persisting it.

### `workflows.proposeCreate`

**Mutability**: `Proposal`
**Purpose**: Create a durable reviewable proposal for a new workflow.

### `workflows.proposeUpdate`

**Mutability**: `Proposal`
**Purpose**: Create a durable reviewable proposal for updating an existing workflow version.

## Runtime Tools

### `instances.search`

**Mutability**: `ReadOnly`
**Purpose**: Find authorized workflow instances by workflow, status, date range, incident presence, or text query.

### `instances.get`

**Mutability**: `ReadOnly`
**Purpose**: Return a model-safe workflow instance summary.

### `instances.getExecutionHistory`

**Mutability**: `ReadOnly`
**Purpose**: Return a bounded activity timeline for an instance.

### `instances.getActivityState`

**Mutability**: `ReadOnly`
**Purpose**: Return bounded state for selected activities in an instance.

### `incidents.search`

**Mutability**: `ReadOnly`
**Purpose**: Find incidents by workflow, instance, activity, time range, or error text.

### `incidents.get`

**Mutability**: `ReadOnly`
**Purpose**: Return a single incident summary with evidence references.

## Deferred Tools

These are intentionally out of MVP and require explicit future approval semantics:

- `instances.proposeRetry`
- `instances.proposeCancel`
- `instances.proposeRestart`
- `workflows.proposeDelete`
- `workflows.proposePublish`
- `workflows.proposeUnpublish`
