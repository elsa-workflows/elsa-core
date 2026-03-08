# Quickstart: Shell Reload API Endpoints

## Goal

Implement and verify two administrative endpoints that refresh shell-backed configuration:

- reload all shells
- reload one requested shell while still using the current full-reload fallback

## Implementation Steps

1. Add the API module dependency required to consume `IShellManager`, `IShellSettingsProvider`, and `IShellSettingsCache`.
2. Add a shell reload orchestration contract and implementation under `src/modules/Elsa.Workflows.Api/Contracts` and `src/modules/Elsa.Workflows.Api/Services`.
3. Add endpoint folders under `src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll` and `src/modules/Elsa.Workflows.Api/Endpoints/Shells/Reload` with collocated request/response models.
4. Use the orchestration service to enforce:
   - busy rejection
   - unknown-shell validation
   - targeted requested-shell strictness
   - detailed per-shell results
   - current full-reload fallback semantics for the targeted endpoint
5. Add a new client resource under `src/clients/Elsa.Api.Client/Resources/Shells` so first-party consumers can call both endpoints.
6. Add component tests under `test/component/Elsa.Workflows.ComponentTests/Scenarios/RestApis/Endpoints/Shells`.

## Verification Scenarios

1. Full reload returns `Completed` when every affected shell refreshes successfully.
2. Full reload returns `Partial` and shell-level detail when at least one shell remains unchanged because of invalid configuration.
3. Targeted reload returns `404` when the requested shell is unknown.
4. Targeted reload returns `422` with shell-level detail when the requested shell does not refresh successfully during the fallback full reload.
5. Either endpoint returns `409` while another reload is already in progress.
6. Either endpoint returns `503` when the configuration provider cannot supply usable shell settings.

## Suggested Validation Commands

```bash
dotnet test test/component/Elsa.Workflows.ComponentTests/
```

If the component suite is too broad during iteration, run the project with a test filter targeting the new shell reload scenario names.