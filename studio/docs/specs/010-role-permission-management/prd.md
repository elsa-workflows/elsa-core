# Elsa Studio Role Permission Management

## 1. Executive summary

Elsa Studio will let authorized administrators browse, create, edit, and safely delete roles using the role and permission APIs supplied by Elsa Core. The feature makes exact permissions, wildcard grants, current coverage, future reach, legacy data, and deletion dependencies understandable without weakening server-side authorization.

Elsa Studio owns the administration experience. Elsa Core remains the source of truth for role persistence, permission evaluation, catalog metadata, wildcard reach, deletion impact, and remediation.

## 2. Problem and opportunity

Elsa Core protects API endpoints with explicit permissions and exposes role-authoring and introspection APIs, but Elsa Studio currently has only placeholder Security navigation. Administrators therefore lack a supported UI for managing which permissions a role grants.

The opportunity is to provide a cautious, source-driven role editor that supports the complete Core model without hiding broad future reach or silently discarding stored grants.

## 3. Product vision

An administrator can sign in to Elsa Studio, see only the role-management actions they are permitted to use, author exact or advanced grants with an accurate explanation of their effect, repair legacy grants deliberately, and delete a role only when Core reports that doing so is safe or the administrator explicitly completes supported remediation.

## 4. Users and actors

### Security administrator

- Browses and searches all roles visible in the current Studio tenant context.
- Creates and edits roles using the permission catalog supplied by Core.
- Reviews broad wildcard reach before saving.
- Repairs unresolved stored grants without losing their original values.
- Inspects and resolves supported deletion dependencies.

### Restricted administrator

- May view roles without being allowed to create, update, or delete them.
- Receives an actionable fail-closed state for direct-route access.
- Never gains authority because a control is merely visible or hidden.

### Elsa Core

- Authoritatively validates and persists roles and grants.
- Evaluates endpoint authorization.
- Supplies permission descriptors, effective caller permissions, wildcard reach, deletion impact, and remediation outcomes.

## 5. Primary journeys

### Browse roles

1. An authenticated administrator opens **Security → Roles**.
2. Studio confirms the Identity shell feature and `identity/roles:view` effective permission.
3. Studio loads the complete, non-paginated role collection.
4. The administrator searches by name, ID, or grant and opens a role.
5. Loading, empty, error, and read-only states remain usable and accessible.

### Create a role

1. An administrator with create permission opens `/security/roles/new`.
2. They enter a required role name; Core generates the role ID.
3. They select concrete permissions and optionally add advanced grants.
4. Studio normalizes the submitted grant set by trimming, ordinal de-duplication, and deterministic sorting.
5. Core validates and creates the role; Studio reloads the stored result.

### Edit exact and advanced grants

1. An administrator opens `/security/roles/{id}`.
2. Exact permissions and advanced grants appear as sibling tabs.
3. Exact selections show whether access is stored directly or covered by a wildcard.
4. Each wildcard row explains its own current coverage and future reach.
5. Unresolved stored grants remain verbatim in **Review and repair** and block saving until explicitly removed or replaced.
6. Recognized catalog entries with `verified: false` remain selectable and visibly unverified but do not themselves block saving.

### Delete a role safely

1. Delete from the list or editor opens the same deletion modal.
2. Studio inspects deletion impact before any mutation.
3. A safe role can be deleted after confirmation.
4. Configuration-owned references block deletion and present actionable configuration guidance.
5. Editable references can be remediated only after explicit selection and confirmation.
6. Studio sends the inspected dependency version with remediation.
7. A version conflict refreshes impact and clears prior confirmation; Studio never retries automatically.
8. An incomplete best-effort outcome reports changed and remaining owners and keeps the role visible.

## 6. Functional requirements

### Navigation and authorization

- **FR-001:** Activate `Elsa.Studio.Security` in Server and WebAssembly hosts and include it in the Studio bundle while preserving `AddSecurityModule()`.
- **FR-002:** Show **Security → Roles** only when `Elsa.Identity.ShellFeatures.Identity` is available and the caller effectively holds `identity/roles:view`.
- **FR-003:** Gate view, create, update, and delete independently using `/identity/me/permissions`.
- **FR-004:** Direct-route access fails closed with an actionable unauthorized or unavailable state.
- **FR-005:** Remove the unfinished Users entry from active Security navigation.

### Role list and editor

- **FR-006:** Provide a searchable, non-paginated role list showing name, ID, permission preview, and permitted actions.
- **FR-007:** Use `/security/roles/new` and `/security/roles/{id}` for one shared editor.
- **FR-008:** Require a role name, let Core generate new IDs, and show an existing ID read-only.
- **FR-009:** Do not display tenant-scope fields or badges; Studio tenancy is implicit in the current context.
- **FR-010:** Normalize submitted grants by trimming, ordinal de-duplication, and deterministic sorting.
- **FR-011:** Preserve Core's last-write-wins behavior because no revision or ETag contract exists for ordinary role updates.

### Exact permissions and advanced grants

- **FR-012:** Render the Core permission catalog as searchable categories with resource metadata and supported-verb controls.
- **FR-013:** Category bulk selection adds only the concrete permissions currently shown and never creates a wildcard.
- **FR-014:** Present **Exact permissions** and **Advanced grants** as sibling tabs, not a timeline or wizard.
- **FR-015:** Store advanced wildcard expressions independently from exact selections.
- **FR-016:** Show reach per advanced wildcard grant, including current coverage and future matching behavior.
- **FR-017:** Show wildcard-covered exact permissions as effective access without silently materializing redundant exact grants.
- **FR-018:** Never remove pre-existing redundant exact grants silently; any cleanup requires explicit review.
- **FR-019:** Preserve unresolved stored grants verbatim and disable Save until each is removed or replaced.
- **FR-020:** Show `verified: false` catalog entries as recognized but unverified.

### Deletion

- **FR-021:** Use one deletion modal from both list and editor.
- **FR-022:** Inspect deletion impact before mutation.
- **FR-023:** Delete directly after confirmation when Core reports the role is safe.
- **FR-024:** Show configuration-owned blockers and guidance without modifying configuration.
- **FR-025:** Offer remediation only for references Core reports as editable.
- **FR-026:** Require explicit confirmation for editable-policy removal, final-default-role replacement, and best-effort execution.
- **FR-027:** Submit the inspected dependency version and require renewed review on conflicts.
- **FR-028:** Never automatically retry a conflicting or partially completed remediation.
- **FR-029:** Retain the role and report changed and remaining owners when remediation is incomplete.

## 7. Non-functional requirements

- **Security:** Hidden or disabled controls are presentation only; Core re-authorizes every mutation. Studio never infers authority from navigation visibility.
- **Accessibility:** Keyboard operation, visible focus, semantic headings, accessible labels, non-color status cues, and responsive dialogs meet WCAG 2.1 AA expectations.
- **Compatibility:** Use relative `/identity/...` routes; backend configuration continues to own the `/elsa/api` prefix.
- **Responsiveness:** The list becomes role cards on narrow screens; the editor becomes single-column; deletion becomes a full-screen mobile dialog.
- **Reliability:** Prevent double submit, cancel outstanding work on disposal, and keep roles visibly retained after incomplete deletion.
- **Data preservation:** Unknown or legacy stored grants are never silently normalized away.
- **Freshness:** Explain that role changes affect users after token refresh or reissuance.

## 8. Domain and conceptual model

- **Exact grant:** A concrete `{resource}:{verb}` permission stored on the role.
- **Advanced grant:** A wildcard expression stored alongside exact grants.
- **Current coverage:** Registered resources and verbs matched by one advanced grant now.
- **Future reach:** Permissions registered later that the wildcard expression will also match.
- **Covered permission:** Effective access derived from an advanced grant and not necessarily stored as an exact grant.
- **Unverified catalog entry:** A recognized descriptor whose catalog metadata is marked `verified: false`.
- **Unresolved stored grant:** A persisted value that cannot be mapped to a recognized catalog entry or supported wildcard expression.
- **Deletion impact:** Core's versioned snapshot of role dependencies and remediation eligibility.

## 9. Product principles

- Make broad access explicit.
- Preserve stored data until the administrator deliberately repairs it.
- Separate effective access from persisted grants.
- Fail closed when capability or authority is unknown.
- Explain blockers with an actionable path.
- Keep tenant context implicit, consistent with the rest of Studio.

## 10. Constraints

- Elsa Core APIs and persistence are unchanged by this program unless discovery proves a contract gap.
- Ordinary role updates have no ETag or revision contract.
- The permission catalog is supplied by Core and must not be hard-coded in Studio.
- User administration is out of scope.
- Studio cannot edit server configuration.
- The full feature requires a current-main-compatible Core backend; older 3.8 release contracts cannot provide catalog, reach, or effective caller permissions.

## 11. Explicit non-goals

- User CRUD or role assignment to users.
- New Core endpoints, persistence, or public server contracts.
- Role-update concurrency control beyond Core's current behavior.
- Automatic migration or normalization of legacy stored grants.
- Editing tenant scope.
- Treating hidden UI as authorization enforcement.

## 12. Delivery stages and demonstrations

### M1 — Program and contract foundation

Demonstrate that canonical artifacts, approved mockups, executable work, module activation, client contracts, and permission-aware navigation are consistent with the live Core contract.

### M2 — Role authoring

Demonstrate sign in → browse/search → create → assign exact and advanced grants → save → reload → update → reload in both Server and WebAssembly hosts.

### M3 — Safe deletion and release proof

Demonstrate safe deletion, configuration blocking, editable remediation, version conflict refresh, incomplete retention, restricted-user rendering, keyboard access, and token-refresh messaging.

## 13. Success criteria

- An authorized administrator completes the full role lifecycle through Studio against a real Core host.
- Restricted users see only the actions they hold and mutations remain server-authorized.
- Wildcard reach and persisted-versus-effective access are understandable in the UI.
- Legacy grants cannot disappear without explicit repair.
- Deletion never proceeds from stale impact or leaves an incompletely remediated role appearing deleted.
- Component, build, browser, accessibility, and end-to-end demonstrations pass for Server and WebAssembly.

## 14. Open product decisions

No material product decision remains open for the first implementation milestone. Discoveries requiring Core changes become separate issues or bounded spikes.
