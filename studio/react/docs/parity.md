# Partial parity audit

This is the evidence checklist for [#8851](https://github.com/elsa-workflows/elsa-core/issues/8851),
part of the 3.11 React Studio Preview. It describes the existing implementation and
confirmed gaps; it is not a declaration of complete Blazor parity. Baseline audited:
`5828b2e09d19abfe198098fbbac481cb734f7a13` on 2026-10-21.

| Area | Evidence and result | Follow-up |
| --- | --- | --- |
| Workflow settings | Actual editor browser test edits name/description, activation, incident and commit strategies, usable-as-activity, consumer auto-update and category through the normal save flow. Log persistence currently writes a field absent from server WorkflowOptions; a real Core save/reload drops it and creates no runtime configuration. | [#8940](https://github.com/elsa-workflows/elsa-core/issues/8940) fixes workflow-level logging. |
| Variable test values | Browser test edits and saves the separate default-value field. Source comparison with Blazor confirms there is no test-value editor for `customProperties.VariableTestValues`, which is keyed by variable ID. | [#8941](https://github.com/elsa-workflows/elsa-core/issues/8941); [#7402](https://github.com/elsa-workflows/elsa-core/issues/7402) is the separate user-scoped storage enhancement. |
| Outcomes | Actual editor browser test adds/removes named workflow outcomes, saves and reloads. Activity input dynamic-outcome tests separately cover JSON encoding, fixed outcomes and preservation of existing connection names. | No gap found in this scope. |
| Version viewer/delete | Actual editor browser test opens a historical version, verifies read-only metadata, returns to latest, confirms deletion of a non-current unpublished version, and checks the version ID sent to the API and list refresh. The UI uses the same definitions/delete permission as the server endpoint. | No gap found in this scope. |
| Applications/API keys | API-client operations exist. Security registers Users and Roles only; no application/API-key route or screen exists. No live credentials were created or logged during the audit. | [#8942](https://github.com/elsa-workflows/elsa-core/issues/8942). |
| Undo/redo | Designer-store tests restore activity settings and connections, clear redo after another edit, and undo nested duplication. Actual canvas keyboard test uses undo/redo after paste and verifies the saved/reloaded graph. | No gap found in those operations. |
| Copy/paste | Store tests verify new IDs/node IDs, deep-copy isolation, internal-edge remapping, exclusion of external edges, cut/undo and nested descendant IDs. Actual canvas keyboard test verifies normal paste/save/reload. Copying Task1 and Task2 together yields two Task3 names, confirmed through the public store API. | [#8944](https://github.com/elsa-workflows/elsa-core/issues/8944) fixes batch name allocation. |
| Activity port providers | Existing Switch slot tests cover discovery, assignment, traversal and fallback. Dynamic-outcome tests verify rendered port identities. Real Core HTTP descriptors plus status codes 200/201 produce only Done flow ports; SendHttpRequest status-code child slots are absent. | [#8943](https://github.com/elsa-workflows/elsa-core/issues/8943) adds HTTP provider parity. |
| Data-usage variable scopes | Executing the immutable imported index and adapted SDK index with same-named variables in sibling Sequences attributes each sibling's reads to both variables. Runtime source resolves names in the current/ancestor context. This is an inherited attribution limitation; no dedicated CLR scope regression is claimed. | [#8978](https://github.com/elsa-workflows/elsa-core/issues/8978). |

The per-activity commit/log/resilience/task/version settings and instance retries
have their own [contract and verification checklist](activity-settings.md).
BPMN, localization, tenant switching and C# completion remain separately owned epic
children. The selectable BPMN designer work in #8929 belongs to its own epic #8898
and explicitly depends on the import/export parity work here.

Evidence lives in `tests/parity.spec.ts`, `packages/sdk/src/designer/store.test.ts`,
the existing specialized-editor/Switch tests, and the sanitized
[reference-backend observations](../../docs/specs/react-studio-preview/parity-observations.json).
The browser tests use the production ESM modules and real ASP.NET host with
controlled API responses; they verify editing and HTTP contracts. The logging
observation uses the real Core API and removes its temporary definition. HTTP
observations use real Core descriptors with local SDK port/slot computation; no
external HTTP activity is executed. These checks do not claim that every production
feature or permission combination has been exercised.

Run `pnpm test`, `pnpm lint`, `pnpm typecheck` and `pnpm test:browser` from
`studio/react` after building the production assets and standalone host as described
in the deployment guide. Update this checklist and its observations as the linked
follow-ups are delivered.
