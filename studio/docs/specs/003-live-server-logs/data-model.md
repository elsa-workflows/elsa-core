# Data Model: Server Logs Studio Module

## ServerLogEvent

- `Id`
- `Timestamp`
- `Level`
- `Category`
- `Message`
- `Exception`
- `TraceId`
- `CorrelationId`
- `TenantId`
- `WorkflowDefinitionId`
- `WorkflowInstanceId`
- `SourceId`
- `Properties`
- `Scopes`

## ServerLogSource

- `Id`
- `DisplayName`
- `ServiceName`
- `PodName`
- `ContainerName`
- `Namespace`
- `NodeName`
- `Status`
- `LastSeen`

## ServerLogFilter

- `MinimumLevel`
- `Levels`
- `CategoryPrefix`
- `Text`
- `TenantId`
- `WorkflowDefinitionId`
- `WorkflowInstanceId`
- `TraceId`
- `CorrelationId`
- `SourceId`
- `From`
- `To`
- `Take`

## ServerLogViewState

- `Filter`
- `IsPaused`
- `AutoScroll`
- `WrapMessages`
- `Compact`
- `ConnectionStatus`
- `VisibleRowCap`
- `LocalDroppedRows`

## Invariants

- `All sources` maps to no `SourceId` filter.
- Changing filters refreshes recent rows and updates the live subscription.
- Disposing the view disposes the active observer.
- Local rows never exceed the configured cap.
