# Executable backlog — Elsa Studio Role Permission Management

## Program

### Elsa Studio Role Permission Management

Deliver the complete administrator journey from permission-aware navigation through role authoring and safe deletion, validated against a real Elsa Core host.

## Epic — UX definition and approval

### Feature — Three design directions

Status: Done. Dedicated-page, split-view, and guided directions were reviewed.

### Feature — Approved responsive interaction specification

Status: Done. The product owner approved separate list/editor surfaces, exact and advanced tabs, per-grant reach, responsive states, and shared deletion dialogs.

## Epic — Role administration

### Feature — Identity contracts and permission-aware navigation

#### Task — Activate the Security module and add internal identity clients

Agent state: Agent Ready after contract discovery is recorded.

Acceptance:

- Security module is active in Server and WebAssembly hosts and included in the bundle.
- Internal clients cover the verified role, catalog, effective-permission, reach, deletion-impact, and remediation routes.
- Relative routes do not hard-code `/elsa/api`.
- Contract tests cover serialization and structured validation/conflict responses.

#### Task — Add effective-permission context and fail-closed navigation

Agent state: Agent Ready after client contracts exist.

Acceptance:

- Roles navigation requires the Identity feature and `identity/roles:view`.
- Direct routes fail closed with an actionable state.
- Create, update, and delete actions are gated independently.
- Users is absent from active Security navigation.

### Feature — Role browse/create/edit

#### Task — Implement the responsive role list

Agent state: Not Ready until navigation/client foundation lands.

Acceptance:

- Searchable non-paginated desktop table and mobile cards match the approved mockup.
- Loading, empty, error, retry, read-only, and permitted-action states are covered.
- Name, ID, permission preview, and actions are shown; tenant scope is absent.

#### Task — Implement the shared create/edit page

Agent state: Not Ready until navigation/client foundation lands.

Acceptance:

- `/security/roles/new` and `/security/roles/{id}` share one editor.
- Name is required, new ID is Core-generated, existing ID is read-only.
- Save is cancellation-aware and double-submit safe.
- Submitted grants are trimmed, ordinally de-duplicated, and sorted.
- Reload shows the persisted Core result.

### Feature — Catalog and wildcard permission authoring

#### Task — Implement exact permission catalog selection

Agent state: Not Ready until the shared editor exists.

Acceptance:

- Catalog is searchable and grouped by category/resource.
- Bulk selection writes current exact grants only.
- Direct, covered, unverified, and ungranted states are distinguishable without relying on color.

#### Task — Implement advanced grants and per-grant reach

Agent state: Not Ready until the shared editor exists.

Acceptance:

- Advanced grants are a standalone sibling tab.
- Global, resource, subtree, and verb wildcard forms are supported according to Core grammar.
- Each resource wildcard queries and displays its own current coverage and future reach.
- Covered exact rows are not silently materialized.

#### Task — Implement legacy grant review and repair

Agent state: Not Ready until catalog and advanced-grant classification exist.

Acceptance:

- Unresolved stored values remain verbatim.
- Save is blocked until every unresolved value is explicitly removed or replaced.
- `verified: false` catalog entries remain recognized and visibly unverified.

## Epic — Safe deletion and release proof

### Feature — Guided role deletion/remediation

#### Task — Implement safe and blocked deletion states

Agent state: Not Ready until role list/editor actions exist.

Acceptance:

- List and editor open the same deletion modal.
- Safe deletion confirms and executes once.
- Configuration blockers show paths and guidance without mutation.
- 403 and 404 outcomes retain coherent UI state.

#### Task — Implement editable remediation and conflict recovery

Agent state: Not Ready until the base deletion modal exists.

Acceptance:

- Only editable references can be selected for remediation.
- Final-default replacement, editable-policy removal, and best-effort execution require explicit confirmation.
- Inspected dependency version is submitted.
- Version conflict refreshes impact and clears confirmation without retry.
- Incomplete outcomes list changed and remaining owners and retain the role.

### Feature — End-to-end validation

#### Task — Validate Server and WebAssembly role lifecycle

Agent state: Not Ready until implementation features are complete.

Acceptance:

- Affected projects and solution build.
- Security tests pass.
- Desktop and mobile browser tests cover keyboard and accessible labels.
- Real create → save → reload → update → reload → delete/remediate journey passes in Server and WebAssembly.
- Restricted-user rendering and server-side anti-escalation are demonstrated.
- Token refresh/reissuance behavior is recorded.
