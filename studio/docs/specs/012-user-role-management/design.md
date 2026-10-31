# User and Role Management UX

## Context

Elsa Server exposes CRUD endpoints for users and roles under `/identity/users` and
`/identity/roles`. Elsa Studio currently contains route shells for both resources,
but no client contracts, navigation entries, or management experience.

The design follows the Administration navigation introduced in feature 011 and
uses the same full-page resource pattern as External Authentication. Secrets is a
useful reference for credential handling, but its create dialog is too narrow for
role assignment and permission editing.

## Recommended design

### Navigation

- Add **Users** and **Roles** as the first two children of **Identity & access**.
- Show each item only when the matching remote Identity feature and read permission
  are available.
- Keep identity provider connections, external identity links, and authentication
  sessions after these core identity resources.

### List pages

- `/security/users` shows name, assigned roles, and tenant scope.
- `/security/roles` shows name, permissions, and tenant scope.
- Both pages provide local search, explicit loading and error states, a meaningful
  empty state, row navigation, keyboard-accessible edit actions, and permission-
  gated create/delete affordances.
- The server endpoints return complete collections without paging or search, so the
  pages load once and filter locally rather than presenting misleading pagination.

### Create and edit pages

- `/security/users/new` and `/security/users/{id}` share one user editor.
- `/security/roles/new` and `/security/roles/{id}` share one role editor.
- Editors use a compact overview card with a single clear primary save action,
  cancel/back navigation, inline validation, and an outlined danger section for
  existing records.
- User names are immutable after creation because the API only updates password and
  roles. The edit page therefore presents the user name as read-only identity.
- Available roles come from the roles endpoint and are selected with a multi-select.
- New passwords are optional. Leaving the create password blank asks the server to
  generate one; Studio shows it exactly once in a dedicated success dialog and never
  logs it. On edit, a blank password means "leave unchanged".
- Role names and permission strings are editable. Because the server has no
  permission-catalog endpoint, permissions use an explicit add/remove token editor
  with examples rather than an incomplete hard-coded checklist.

### Deletion

- Delete actions always require confirmation and are permission-gated.
- Conflict responses are surfaced as actionable errors rather than silently
  removing the row.
- Advanced cross-module role-remediation endpoints are intentionally out of scope
  for the first management UX; a blocked delete remains non-destructive and explains
  that dependencies must be resolved.

## API and state design

- Add narrow Refit contracts and local DTOs in the Security module.
- Resolve clients through `IBackendApiClientProvider`, matching existing Studio
  modules.
- Centralize permission-claim lookup for the Security pages and menu contributor.
- Never retain hashed password fields returned by older server contracts; deserialize
  only the fields the UI needs.

## Verification

- bUnit tests cover routes, menu permission gates, list rendering/filtering,
  create/update payloads, generated-password disclosure, and deletion confirmation.
- Build and test the affected solution projects.
- Test-drive navigation, list, create, edit, validation, responsive layout, and
  keyboard interaction in the running Studio with Computer Use.
