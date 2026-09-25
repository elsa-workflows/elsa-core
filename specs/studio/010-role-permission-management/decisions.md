# Decision log — Elsa Studio Role Permission Management

## 2026-09-01 — Full role CRUD

**Decision:**
Studio will support browsing, creating, editing, and deleting roles. User administration remains out of scope.

**Rationale:**
The Core contract already exposes the complete role lifecycle, and deletion requires first-class safety UX rather than an external workaround.

**Consequences:**
- Create, update, and delete are independently permission-gated.
- The unfinished Users navigation entry is removed.

**Revisit:** No planned revisit.

## 2026-09-01 — Catalog plus explicit advanced grants

**Decision:**
Exact permissions and advanced wildcard grants are sibling tabs in the shared role editor. Advanced grants are stored independently from exact selections.

**Rationale:**
Most administrators need catalog selection, while wildcard expressions require explicit explanation of their broad and future effect.

**Consequences:**
- No timeline or wizard is used for routine editing.
- Reach is shown per advanced grant row.
- Covered catalog rows distinguish effective access from stored exact grants.

**Revisit:** Revisit only if usability testing shows administrators cannot relate the two tabs.

## 2026-09-01 — Exact-only category bulk selection

**Decision:**
Category bulk controls add only currently displayed concrete permissions and never synthesize a wildcard.

**Rationale:**
Bulk selection should not silently grant future reach.

**Consequences:**
- The UI reports how many concrete grants will be added.
- Wildcards are created only from Advanced grants.

**Revisit:** No planned revisit.

## 2026-09-01 — Preserve unresolved and redundant stored values

**Decision:**
Unresolved stored grants remain verbatim and block Save until removed or replaced. Existing redundant exact grants are not silently removed when a wildcard covers them.

**Rationale:**
Role administration must not destroy unfamiliar or intentionally redundant security data through normalization.

**Consequences:**
- Repair is explicit.
- New wildcard coverage does not materialize new redundant exact grants.
- Optional cleanup requires an administrator-visible choice.

**Revisit:** No planned revisit.

## 2026-09-01 — Guided deletion and remediation

**Decision:**
List and editor use one deletion modal. Safe deletion, configuration blockers, editable remediation, dependency-version conflicts, and incomplete outcomes are distinct states.

**Rationale:**
Deletion is a focused, blocking transaction whose safety depends on Core's impact snapshot.

**Consequences:**
- Configuration is never modified by Studio.
- Editable remediation requires explicit confirmation.
- Conflicts refresh impact and clear confirmation without automatic retry.
- Incomplete outcomes retain the role visibly.

**Revisit:** No planned revisit.

## 2026-09-01 — Tenant context remains implicit

**Decision:**
Do not show tenant-scope fields, columns, or badges in role administration.

**Rationale:**
Studio already operates in a tenant context; exposing it only on these screens would be arbitrary and inconsistent.

**Consequences:**
- The role list shows name, ID, permissions, and actions only.
- The editor shows role name and existing ID only.

**Revisit:** Revisit only as part of a Studio-wide tenant-context design change.

## 2026-09-01 — Separate list, details, and deletion surfaces

**Decision:**
Use a standalone list page, standalone create/edit page, and shared modal deletion workflow.

**Rationale:**
Permission authoring needs space, while deletion needs focused confirmation and remediation without combining unrelated tasks into one workspace.

**Consequences:**
- Routes are `/security/roles`, `/security/roles/new`, and `/security/roles/{id}`.
- Mobile deletion is a full-screen dialog.

**Revisit:** No planned revisit.

## 2026-09-01 — Two mockup gates approved

**Decision:**
The product owner approved the A+C direction and the final responsive list, editor-tab, deletion, and remediation mockups.

**Rationale:**
The program required visual direction and final-design approval before production UI work.

**Consequences:**
- Implementation may begin.
- Material visual deviations return to the product owner.

**Revisit:** Only for material deviations from the approved mockups.

## 2026-09-01 — No ADR initially

**Decision:**
Do not create an ADR for the initial implementation.

**Rationale:**
The feature uses existing Studio modules, backend-client conventions, MudBlazor components, and Core authorization contracts.

**Consequences:**
- A durable architectural discovery triggers a bounded spike and proposed ADR instead of silent scope expansion.

**Revisit:** When discovery exposes a cross-module or public-contract decision.

## 2026-09-01 — Dedicated Project instead of Foundation scoped-RBAC board

**Decision:**
Track this delivery in a dedicated Elsa Studio Project rather than the existing organization Project **Scoped role-based access**.

**Rationale:**
The existing Foundation-wide program targets a future authorization model that forbids ordinary wildcard role grants and requires role revisions or ETags. This feature intentionally targets the current `elsa-core` Identity contract, which supports wildcard grants and last-write-wins role updates.

**Consequences:**
- The two programs do not share Agent Ready tasks or completion claims.
- A future migration to the Foundation authorization model remains separate compatibility work.

**Revisit:** When Elsa Studio changes its backend from the current `elsa-core` contract to the Foundation scoped-RBAC contract.
