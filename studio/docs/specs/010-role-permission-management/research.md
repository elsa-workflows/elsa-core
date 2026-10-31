# Discovery — Elsa Studio Role Permission Management

## Studio current state

- `Elsa.Studio.Security` exists but contains placeholder Roles and Users pages.
- The module is in the solution but not active in the Studio bundle, Server host, or WebAssembly host.
- `AddSecurityModule()` currently registers only the feature and unconditional menu provider.
- Existing feature-gated menus use `IRemoteFeatureProvider.IsEnabledOrDefaultAsync` and fail closed.
- No central Identity permission service or `/identity/me/permissions` client exists on the current Studio branch.
- Existing internal clients use Refit contracts registered with `AddRemoteApi<TApi>(BackendApiConfig)` and relative routes.
- Existing list, tab, cancellation/disposal, mutation guard, and bUnit patterns can be reused; no current browser-test framework is present.

## Verified current Core contract

All required routes exist in `/Users/sipke/Projects/Elsa/elsa-core-main`:

- `GET /identity/roles`
- `POST /identity/roles`
- `PUT /identity/roles/{id}`
- `DELETE /identity/roles/{id}`
- `GET /identity/permissions`
- `GET /identity/permissions/reach?resource=...`
- `GET /identity/me/permissions`
- `GET /identity/roles/{id}/deletion-impact`
- `POST /identity/roles/{id}/remove-from-jit-policies-and-delete`

Core generates a new role ID from the kebab-cased name when Studio omits `id`. Core does not trim, de-duplicate, or sort permission lists, so Studio owns the approved normalization. Ordinary role updates remain last-write-wins.

The catalog supplies resource, supported verbs, non-core verbs, display name, description, category, and `verified`. Reach accepts one resource pattern and reports currently matched resources. Studio combines that point-in-time report with the stored verb pattern and catalog metadata to explain each advanced grant.

`/identity/me/permissions` reflects token claims and returns resolved concrete verbs for every registered resource. Role changes affect an existing user after token refresh or reissuance.

## Deletion contract

Impact supplies:

- opaque `dependencyVersion`;
- `executionMode` (`atomic` or `bestEffort`);
- `canDelete` and `canRemediate`;
- configuration references;
- editable references;
- warnings.

Remediation submits:

- `expectedDependencyVersion`;
- `confirmRemoveFromEditableJitPolicies`;
- `confirmEmptyDefaultRoles`;
- `confirmBestEffort`.
- optional `selectedReferences`, each identified by the contributor `source` and `ownerId`;
- optional `replacementRoleId` when a selected edit would otherwise remove a policy's final default role.

Omitting `selectedReferences` preserves the legacy all-editable contract. Sending an explicit empty collection performs no editable mutations and cannot delete while dependencies remain. Unknown, duplicate, stale, or non-database selections fail closed. Configuration-owned dependencies block the complete operation even when database-owned references were selected.

Core maps deletion outcomes to `204`, `400 confirmation_required`, `403`, `404 not_found`, and `409` codes including `role_referenced_by_jit_policy`, `role_dependency_changed`, and `role_remediation_incomplete`. Core retains the role after incomplete remediation and does not automatically retry version conflicts.

## Compatibility boundary

The Studio checkout's nearby release Core (`release/3.8.0`) and package fallback do not expose the approved catalog, reach, or caller-permission endpoints and still use legacy permission names. Implementation and end-to-end validation therefore require a current-main-compatible Core backend. This is an explicit mixed-version compatibility boundary, not a Studio fallback opportunity.

## Core discoveries captured separately

These are not silently expanded into the Studio implementation:

1. The in-memory role path does not safely isolate deterministic role IDs across tenants. Real-host validation also proved that a newly created null-tenant role is omitted by an immediate authenticated list when an ambient tenant is active, so #8012 is a hard acceptance prerequisite rather than a release-note-only boundary.
2. External-authentication deletion impact is not tenant-filtered.
3. Role deletion does not publish the security notification emitted by create/update.

Tracking issues:

- [elsa-core#8012](https://github.com/elsa-workflows/elsa-core/issues/8012)
- [elsa-core#8013](https://github.com/elsa-workflows/elsa-core/issues/8013)
- [elsa-core#8014](https://github.com/elsa-workflows/elsa-core/issues/8014)

Studio implementation may proceed against a current-main-compatible, appropriately isolated deployment, but release proof must record this boundary. The Core defects are tracked separately.

The current Core main branch also had an unrelated .NET 10 compile break in the UserTasks requestless invitation endpoint. That gate is isolated as #8030/PR #8031 and must pass independently before the role-remediation PR's repository-wide CI can be interpreted.

## Architectural conclusion

No Studio ADR is required. Existing module, Refit, feature-provider, MudBlazor, and test patterns are sufficient. A Core contract change or a future move to the Foundation scoped-RBAC model would require separate compatibility work.
