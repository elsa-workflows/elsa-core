# Phase 0 Research: Shell Reload API Endpoints

## Decision 1: Keep the feature in the existing API and client modules

- **Decision**: Implement the endpoints in `Elsa.Workflows.Api` and add the typed client surface in `Elsa.Api.Client`.
- **Rationale**: The repository already places administrative REST endpoints in `Elsa.Workflows.Api`, and component tests exercise those endpoints through the hosted API fixture. A matching client resource keeps first-party consumers aligned with the new API surface.
- **Alternatives considered**:
  - Add the endpoints to `Elsa.Tenants`: rejected because the capability is broader than tenant CRUD and the requested change is centered on the existing API surface used by Elsa clients.
  - Create a new shell-administration module: rejected as unnecessary module growth for a two-endpoint feature.

## Decision 2: Introduce a shell reload orchestration service in the API module

- **Decision**: Add a dedicated orchestration contract and service in `Elsa.Workflows.Api` that the endpoints call instead of embedding reload logic directly inside endpoint classes.
- **Rationale**: The spec requires busy-state rejection, unknown-shell validation, per-shell result reporting, and a future upgrade path from full-reload fallback to selective reload. Encapsulating that logic in one service keeps endpoints conventionally thin and localizes future upstream changes.
- **Alternatives considered**:
  - Inject `IShellManager` directly into each endpoint: rejected because the same branching, validation, and result-mapping logic would be duplicated.
  - Put the orchestration service in `src/common/`: rejected because the behavior is API-facing, request-oriented, and not shared elsewhere yet.

## Decision 3: Use `IShellSettingsProvider` and `IShellSettingsCache` as the shell identity inputs

- **Decision**: Validate shell IDs and derive per-shell reload scope from the authoritative shell settings provider plus the current shell settings cache.
- **Rationale**: Upstream CShells uses `IShellSettingsProvider.GetShellSettingsAsync` as the source for `ReloadAllShellsAsync`, and `IShellSettingsCache` provides the current in-memory view of known shells. Using both lets Elsa compare requested/current/latest state without guessing.
- **Alternatives considered**:
  - Validate only against the current cache: rejected because removed or newly added shells would be misclassified relative to the latest configuration source.
  - Validate against shell host instances only: rejected because shell host state is runtime-focused and not the authoritative source for latest configuration.

## Decision 4: Implement the current fallback as a full reconciliation flow, not a blind call to `ReloadAllShellsAsync`

- **Decision**: Plan the current implementation around a full shell reconciliation flow that can still represent the current full-reload fallback semantics while producing detailed outcomes and partial application.
- **Rationale**: Upstream `DefaultShellManager.ReloadAllShellsAsync` updates the shell settings cache and publishes `ShellsReloaded`, and the ASP.NET Core notification handler re-registers endpoints for all shells. That path does not expose per-shell success/failure details, requested-shell strictness, or partial application behavior required by the clarified spec. A reconciliation-oriented orchestration can compose existing manager operations (`AddShellAsync`, `UpdateShellAsync`, `RemoveShellAsync`) and preserve the current full-reload fallback for targeted requests.
- **Alternatives considered**:
  - Call `ReloadAllShellsAsync` directly for both endpoints: rejected because it cannot distinguish requested-shell failure from overall success and does not support the spec’s partial-success response model.
  - Implement a targeted one-shell update now: rejected because the user explicitly wants the targeted endpoint to remain on full-reload fallback behavior until upstream support exists.

## Decision 5: Use a shared response model with top-level status plus per-shell outcomes

- **Decision**: Both endpoints will return the same logical result shape: a top-level reload status, optional requested shell ID, and a collection of per-shell outcome records.
- **Rationale**: The spec now requires detailed shell-level reporting for both full and targeted requests. A shared response model keeps the API and client simpler and makes targeted fallback behavior transparent when other shells are also affected.
- **Alternatives considered**:
  - Minimal boolean success responses: rejected because they cannot represent partial success or requested-shell strict failures.
  - Different response schemas per endpoint: rejected because both endpoints expose the same operational data with different scopes.

## Decision 6: Keep endpoint request/response models collocated and move shared orchestration types into contracts

- **Decision**: Keep endpoint request/response models collocated with each endpoint and place only shared orchestration result types in `Elsa.Workflows.Api/Contracts`.
- **Rationale**: This preserves the repository’s single-endpoint-per-class convention while still avoiding duplication for shared internal result types consumed by both endpoints.
- **Alternatives considered**:
  - Put all reload models in a shared `Endpoints/Shells/Models.cs`: rejected because it weakens endpoint colocation and drifts from existing API conventions.
  - Duplicate all result types per endpoint: rejected because both endpoints share the same outcome model and would diverge unnecessarily.

## Decision 7: Cover the feature with component tests, not a new unit-test project

- **Decision**: Add component tests under `Elsa.Workflows.ComponentTests` for the new endpoints and their observable runtime behavior.
- **Rationale**: There is no existing dedicated `Elsa.Workflows.Api` unit-test project, and the repository already validates REST endpoint behavior through the hosted `WorkflowServer` fixture with authenticated HTTP clients. Component tests can verify routes, permissions, status handling, and the effect on active shell-backed behavior in one place.
- **Alternatives considered**:
  - Create a brand-new `Elsa.Workflows.Api.UnitTests` project: rejected as out of scope for this feature and unnecessary for the first increment.
  - Rely only on integration tests against lower-level services: rejected because the feature’s primary value is the external API behavior.
