# Data Model: Weaver Grounding Tools

## ActivityGroundingSummary

Model-safe description of an installed activity.

**Fields**: `TypeName`, `Version`, `DisplayName`, `Description`, `Namespace`, `Categories`, `IsBrowsable`, `CanStartWorkflow`, `Inputs`, `Outputs`, `Constraints`, `Provider`.

**Rules**:

- Values are derived from Activity Registry descriptors.
- Descriptor details are reduced to model-safe metadata; runtime-only or sensitive implementation details are omitted.
- Multiple versions are represented explicitly.

## ActivityPortSummary

Model-safe input or output descriptor for an activity.

**Fields**: `Name`, `DisplayName`, `Description`, `Type`, `IsRequired`, `IsArray`, `SupportedSyntaxes`, `DefaultValueSummary`, `Category`.

**Rules**:

- Default values are summarized or redacted.
- Type names are stable enough for authoring guidance but do not expose unsafe internals.

## WorkflowGroundingSummary

Model-safe view of a workflow definition or version.

**Fields**: `DefinitionId`, `VersionId`, `Name`, `Version`, `Status`, `IsLatest`, `IsPublished`, `Activities`, `Connections`, `Variables`, `Inputs`, `Outputs`, `TriggerActivities`, `Warnings`.

**Rules**:

- Returned only after authorization checks.
- Large workflow graphs are summarized with optional detail lookup.
- Missing activity descriptors are called out as warnings.

## WorkflowDraftProposalContext

Information used to create or validate a workflow proposal.

**Fields**: `Kind`, `ConversationId`, `BaselineDefinitionId`, `BaselineVersionId`, `DraftPayload`, `Rationale`, `Warnings`, `ValidationDiagnostics`, `GraphDiff`.

**Rules**:

- All writes remain proposals until approved and applied.
- Baseline version is required for updates.
- Drafts must validate against installed activity descriptors before apply.

## RuntimeInstanceGroundingSummary

Model-safe view of workflow instance runtime data.

**Fields**: `InstanceId`, `WorkflowDefinitionId`, `WorkflowVersionId`, `Status`, `SubStatus`, `CreatedAt`, `UpdatedAt`, `FinishedAt`, `CurrentActivityIds`, `Timeline`, `Incidents`, `VariableSummaries`, `InputSummary`, `OutputSummary`.

**Rules**:

- Requires instance read permission.
- Variables and input/output payloads are redacted and size-limited.
- Detailed execution history can be paged or summarized.

## IncidentGroundingSummary

Model-safe incident/error evidence.

**Fields**: `IncidentId`, `InstanceId`, `ActivityId`, `ActivityType`, `Message`, `ExceptionType`, `Timestamp`, `Evidence`, `Severity`.

**Rules**:

- Messages are redacted.
- Evidence references should be enough for Studio drill-in without dumping full logs into model context.

## GroundingToolResult

Common shape for bounded tool responses.

**Fields**: `Summary`, `Items`, `TotalCount`, `HasMore`, `Cursor`, `Warnings`, `EvidenceReferences`.

**Rules**:

- Tool results must fit configured size limits.
- Large result sets return summaries plus cursors or filters.
- Results are suitable for both Copilot tool callbacks and Studio tool activity rendering.

## GroundingCapabilityDescriptor

Provider-neutral capability advertised to Studio.

**Fields**: `Name`, `DisplayName`, `Description`, `ToolNames`, `SupportedAttachmentKinds`, `Enabled`, `DisabledReason`.

**Rules**:

- Studio uses this to enable or disable context pickers and chat actions.
- No provider SDK details are included.
