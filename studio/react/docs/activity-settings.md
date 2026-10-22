# Activity settings and retry history

The activity properties panel includes the following Blazor-compatible settings.
All controls honor the designer's read-only state. Changes use the designer store,
so normal save, undo and redo retain the complete activity and unrelated plugin data.
Merely opening a tab does not modify a definition. Strategy catalog failures show an
error while preserving local Inherit/Default, clear and expression editing actions;
only available catalog options are offered, and read-only mode still disables edits.

| Blazor tab | React tab and visibility | Persisted server contract | Verification |
| --- | --- | --- | --- |
| Commit Strategy | Commit strategy; all activities | `customProperties.commitStrategyName`; catalog uses `name`, not `typeName`; clear also removes the legacy PascalCase alias | Browser selection, immutable-update test, real Core save/reload |
| Log | Log; all activities | `customProperties.logPersistenceConfig`: default, internal state, and camelCase input/output rules | Browser strategy/expression changes, legacy migration tests, real Core save/reload |
| Task | Task; `kind: Task` | `customProperties.runAsynchronously` | Browser checkbox/read-only checks, real Core save/reload |
| Resilience | Resilience; `Resilient` descriptor and installed backend feature (catalog failures retain compatibility) | `customProperties.resilienceStrategy`: Identifier/strategyId or Expression/expression | Browser strategy selection, real Core expression save/reload |
| Version | Version; `RootType: WorkflowDefinitionActivity` | Activity `version` and `workflowDefinitionVersionId` change together | Browser catalog eligibility/read-only checks, paired-version unit test and real Core save/reload |
| Instance Retries | Retries in the selected activity's details; resilient descriptor or saved policy, and `resilience/retries:view` permission | `GET resilience/retries/{activityInstanceId}?skip=…&take=…` | Actual viewer browser test with multiple executions, two pages, attempt details and empty history; encoded ID/auth/tenant/cancellation API test |

The pre-existing asynchronous control remains in Common for other activity kinds.
Additional module-provided activity tabs continue to render through the SDK.

Log rules select a catalog strategy or an expression returning `LogPersistenceMode`
(`Inherit`, `Include`, `Exclude`; numeric enum values are accepted when reading).
They do not use booleans. Strategy identities come from the server catalog.
Legacy `logPersistenceMode` default/input/output modes are read without mutation and
upgraded to assembly-qualified built-in strategy identities when settings change.
Non-null current rules take precedence. Clearing writes null and removes only the
corresponding legacy entry, because the runtime falls back to legacy values for a
null rule. Unknown properties and unrelated historical rules are retained.

Workflow activity versions use the `AllVersions` query. A version can be selected
only when its type and version are present in the activity catalog; the current
activity inputs and custom properties survive the change. The list is paginated. Toolbox creation normalizes the server catalog's CLR-named
workflow definition construction fields to the camelCase activity JSON fields,
without retaining conflicting aliases.

Retry history uses the execution record ID, rather than the graph activity/node ID.
Repeated executions of one node can therefore be inspected separately. Switching
execution resets the page and details; changing page clears the selected attempt.
Queries share the host's authentication, tenant and query scope, cancel on unmount,
and are invalidated with their workflow instance. Loading, API errors, missing
server support, no executions and empty history have explicit states. Opening other
tabs does not fetch retries.

The browser tests in `tests/activity-settings.spec.ts` load the production ESM
modules through the real ASP.NET host and substitute deterministic API responses.
They cover rendering and HTTP contracts; they do not claim to exercise server retry
execution. The separate reference-backend check creates and publishes two reusable
workflow versions, saves a parent with commit/log/resilience/task settings, reloads
and verifies every setting and version ID. It also exercises the dedicated publish,
retract, version revert and reference-update actions before removing its temporary definitions.
The paired-container CI check runs this test against the reference backend.


To repeat the reference-backend check, set `ELSA_TEST_API_URL`,
`ELSA_TEST_USERNAME` and `ELSA_TEST_PASSWORD` for a disposable development server,
then run `pnpm test:settings-server`. The test needs workflow create, publish, view,
retract, update-references and delete permissions and creates temporary published definitions. Credentials
remain in memory; temporary definitions are deleted in its cleanup handler.
