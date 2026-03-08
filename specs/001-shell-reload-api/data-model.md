# Data Model: Shell Reload API Endpoints

## Entity: Shell Reload Command

- **Purpose**: Represents an administrative request to refresh shell runtime state.
- **Fields**:
  - `Scope`: `All` or `Targeted`.
  - `RequestedShellId`: optional shell identifier; required when scope is `Targeted`.
  - `RequestedAt`: execution timestamp captured for result reporting and diagnostics.
- **Validation rules**:
  - `RequestedShellId` is required for targeted commands.
  - Shell identifiers are matched case-insensitively to align with `ShellId` semantics in CShells.

## Entity: Shell Reload Result

- **Purpose**: Represents the top-level outcome returned by either reload endpoint.
- **Fields**:
  - `Status`: one of `Completed`, `Partial`, `Failed`, `Busy`, `NotFound`, or `RequestedShellFailed`.
  - `RequestedShellId`: optional shell identifier echoed for targeted requests.
  - `Shells`: collection of `Shell Reload Item Result` records for each affected shell.
  - `ReloadedAt`: timestamp indicating when the orchestration completed.
- **Validation rules**:
  - `Shells` must be populated for `Completed`, `Partial`, and `RequestedShellFailed` outcomes.
  - `RequestedShellId` must be populated for `Targeted` requests.

## Entity: Shell Reload Item Result

- **Purpose**: Describes the shell-level result used to explain a full or targeted reload outcome.
- **Fields**:
  - `ShellId`: shell identifier.
  - `Outcome`: one of `Reloaded`, `Unchanged`, `Removed`, `InvalidConfiguration`, `Unknown`, or `Skipped`.
  - `Requested`: boolean indicating whether this item is the caller’s explicitly requested shell.
  - `Message`: optional human-readable detail describing why the shell remained unchanged or failed.
- **Validation rules**:
  - Each `ShellId` appears at most once per response.
  - `Requested` is true for at most one item.

## Entity: Shell Reload Scope Snapshot

- **Purpose**: Internal planning concept representing the latest provider settings compared with the current cache before orchestration runs.
- **Fields**:
  - `CurrentShellIds`: current in-memory shell identifiers from the settings cache.
  - `LatestShellSettings`: latest shell settings from the provider.
  - `MissingShellIds`: current shells absent from the latest provider view.
- **Why it matters**: The orchestration needs this snapshot to distinguish updates, additions, removals, unknown shell requests, and unchanged shells.

## State Transitions

### Full Reload Command

`Pending` → `Completed`
when all affected shells refresh successfully.

`Pending` → `Partial`
when at least one shell refreshes successfully and at least one shell remains unchanged because of invalid configuration or a shell-specific failure.

`Pending` → `Busy`
when another reload is already in progress.

`Pending` → `Failed`
when the shell settings provider cannot supply usable configuration for the operation.

### Targeted Reload Command

`Pending` → `NotFound`
when the requested shell ID does not exist in the authoritative configuration view.

`Pending` → `Busy`
when another reload is already in progress.

`Pending` → `Completed`
when the current full-reload fallback completes and the requested shell refreshes successfully.

`Pending` → `RequestedShellFailed`
when the fallback full reload updates some shells but the requested shell remains unchanged because its updated configuration could not be applied.

`Pending` → `Failed`
when the shell settings provider cannot supply usable configuration for the operation.
