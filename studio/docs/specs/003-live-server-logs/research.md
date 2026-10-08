# Research: Server Logs Studio Module

## R1: Module placement

**Decision**: Add a dedicated `Elsa.Studio.ServerLogs` module.

**Rationale**: Server logs are operational diagnostics, not solely workflow diagnostics. A dedicated module can be linked from workflow views without being coupled to them.

## R2: Live transport

**Decision**: Use SignalR plus REST backfill from the paired Core feature.

**Rationale**: Studio already has SignalR auth plumbing for workflow instance observation. REST backfill prevents the live connection from needing replay semantics.

## R3: UI density

**Decision**: Use a compact log table/list with expandable rows rather than a terminal emulator.

**Rationale**: Structured logs need filters, source badges, exception expansion, and copy/export controls. A terminal emulator would fight those needs.

## R4: Cluster source controls

**Decision**: Treat source as a normal first-class filter with `All sources` default.

**Rationale**: This keeps merged view and pod/process view in one mental model and one page.

## R5: URL state

**Decision**: Preserve primary filters in query string.

**Rationale**: Operators should be able to share "errors for this workflow instance from this pod" links.

## R6: Rendering strategy

**Decision**: Cap local rows first; introduce virtualization if the first implementation is not smooth enough.

**Rationale**: A hard local cap is simple and necessary regardless of virtualization.
