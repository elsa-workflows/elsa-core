# Milestone validation — Elsa Studio Role Permission Management

Validation date: 2026-09-06

Studio candidate: `codex/issue-989-role-lifecycle-corrections`

Core baseline: `5f692b956dd53081d90b5f6ab0d9d283774b9cb0`

## Shared preconditions

- Studio Server (`https://localhost:7113`) and WebAssembly (`https://localhost:7052`) were built from the candidate working tree on .NET 10.
- An isolated Elsa ModularServer ran from the exact Core baseline on `https://localhost:7294/elsa/api` with SQLite persistence.
- WASM used the repository's loopback-only HTTPS CORS forwarding seam on `https://localhost:5001/elsa/api`; Core still performed every authentication, authorization, read, and mutation.
- The Core fixture was explicitly enabled to contribute one `Verified=false` permission descriptor. The deletion fixture used deterministic, isolated role and connection IDs.
- No production or shared database was used. Test credentials and tokens remained runtime-only and were excluded from URLs and captured diagnostics.

## M1 — Program and contract foundation

### Promised outcome

Studio has a verified, permission-aware Security module foundation aligned with the live Core contract.

### Actor

Administrator, restricted authenticated user, and unauthenticated user in an Identity-absent host composition.

### Demonstration

1. Start both Studio hosts against the isolated Identity-enabled ModularServer.
2. Sign in through the real Core Identity endpoint and load the installed-feature and effective-permission catalogs.
3. Confirm Security → Roles appears only when Identity is available and the actor holds `identity/roles:view`.
4. Confirm a restricted actor can open the role list, sees no mutation controls, and receives Core `403` for a direct create attempt.
5. Disable Identity and DefaultAuthentication in the ModularServer shell and confirm both Studio hosts remove the navigation entry and fail closed on direct role routes at 320, 768, 1024, and 1440 px.
6. Run client and component tests for the role API contracts, permission gate, cancellation behavior, and inaccessible-state handling.

### Expected evidence

- Real installed-feature and effective-permission responses control the UI.
- The restricted user retains read-only access while Core remains the mutation authority.
- Identity absence never exposes the role surface.
- Browser diagnostics contain no secrets in URLs and no unexpected 4xx/5xx or console errors.

### Failure / recovery checks

- Missing feature data, missing effective permissions, unauthenticated state, and canceled asynchronous access checks all fail closed.
- Existing access tokens are not silently re-authorized after a role change; the new grant becomes effective only after refresh/reissuance.

### Result

PASS.

### Evidence

- Identity-absent matrix: 8/8 browser checks passed (Server + WASM × 320/768/1024/1440).
- Restricted actor and Core rejection passed in both host matrices; the refreshed-token scenario passed at every required width.
- `dotnet test src/framework/Elsa.Studio.Core.Tests/Elsa.Studio.Core.Tests.csproj --framework net10.0 --no-build --no-restore`: 28/28.
- `dotnet test src/modules/Elsa.Studio.Security.Tests/Elsa.Studio.Security.Tests.csproj --framework net10.0 --no-build --no-restore`: 87/87.
- `dotnet test src/modules/Elsa.Studio.Administration.Tests/Elsa.Studio.Administration.Tests.csproj --framework net10.0 --no-build --no-restore`: 17/17.

### Follow-up

None.

## M2 — Role authoring

### Promised outcome

An authorized administrator can browse, create, reload, edit, and understand exact and advanced grants without losing stored security data.

### Actor

Authenticated administrator with the real Core role-management permissions.

### Demonstration

1. Open Security → Roles and search the complete persisted role list.
2. Create a role with exact grants plus `identity/roles:*` as an advanced grant.
3. Reload the editor and verify the persisted direct grants, current expansion, and future-reach explanation.
4. Rename and save the role, reload again, and verify the stable role ID and updated name through both UI and Core API.
5. Load a legacy unresolved grant, verify it remains verbatim, blocks Save, and offers explicit repair instead of silent data loss.
6. Delete the dependency-free role through the confirmation flow.
7. Repeat lifecycle, search, keyboard, responsive-layout, restricted-actor, token-refresh, and unverified-catalog scenarios in Server and WASM at 320, 768, 1024, and 1440 px.
8. Run strict axe checks and require clean console, network, server-error, and secret-URL diagnostics.

### Expected evidence

- Core persists and returns direct grants unchanged.
- Advanced grants explain both current and future reach.
- The list switches from cards to a table at the intended breakpoint and remains keyboard operable.
- Recognized `Verified=false` catalog entries remain selectable.

### Failure / recovery checks

- Empty searches render an explicit filtered-empty state.
- Unresolved legacy grants cannot be silently saved or discarded.
- A missing shipped WASM PKCE asset, missing document language, and insufficient default-theme navigation contrast were surfaced by the strict browser gate, corrected, and rerun successfully.

### Result

PASS.

### Evidence

- Server normal matrix: 24/24 scenarios passed across the four required widths.
- WASM normal matrix: 22/24 scenarios passed in the complete run; its two navigation axe failures were fixed and both reran successfully. The resulting aggregate is 24/24 at the tested candidate state.
- Dedicated external-authentication browser regression suite: 17/17 passed, including S256 PKCE.
- `dotnet build Elsa.Studio.sln --framework net10.0 --no-restore`: succeeded with 0 errors and 4 pre-existing nullability warnings.
- Responsive screenshots: [Server 320](evidence/2026-09-06/server-320-roles-list.png), [Server 768](evidence/2026-09-06/server-768-roles-list.png), [Server 1024](evidence/2026-09-06/server-1024-roles-list.png), [Server 1440](evidence/2026-09-06/server-1440-roles-list.png), [WASM 320](evidence/2026-09-06/wasm-320-roles-list.png), [WASM 768](evidence/2026-09-06/wasm-768-roles-list.png), [WASM 1024](evidence/2026-09-06/wasm-1024-roles-list.png), and [WASM 1440](evidence/2026-09-06/wasm-1440-roles-list.png).

### Follow-up

None.

## M3 — Safe deletion and release proof

### Promised outcome

An authorized administrator can delete a safe role or remediate editable dependencies without bypassing configuration blockers or stale impact.

### Actor

Authenticated administrator operating on isolated configuration-owned and database-owned dependency fixtures.

### Demonstration

1. Delete a dependency-free role after reviewing the explicit confirmation and existing-token warning.
2. Inspect a configuration-owned dependency and confirm the dialog names the blocker without offering mutation.
3. Inspect editable database-owned references, confirm remediation, submit the inspected dependency version, and verify the role is deleted only after all references change.
4. Mutate a referenced connection after inspection and verify Core returns a version conflict; Studio clears confirmation and renders refreshed impact.
5. Abort the second reference update in a transaction trigger and verify Studio reports changed and remaining owners while retaining the role.
6. Open a role containing an unresolved legacy grant and verify Save remains blocked until explicit repair.
7. Verify a restricted actor cannot invoke deletion and Core rejects a direct mutation.

### Expected evidence

- Configuration-owned references are immutable from Studio.
- Editable references require explicit per-owner confirmation and optimistic dependency versioning.
- Conflict and partial-remediation outcomes never imply that the role was deleted.
- Safe deletion removes the persisted role and returns to the list.

### Failure / recovery checks

- An intentionally incorrect local trigger invocation returned `404` and changed no fixture state; rerunning with the fixture tool's emitted `/role-management/...` paths passed both race scenarios.
- The partial-remediation fixture proves the role remains present and the operator receives both changed-owner and remaining-owner details.

### Result

PASS.

### Evidence

- Seeded real-host deletion outcomes: 5/5 passed at Server 1440 (configuration blocker, editable remediation, conflict refresh, incomplete remediation, unresolved grant).
- Dependency-free deletion passed in every Server and WASM lifecycle scenario at all four widths.
- Core prerequisite PRs #8029, #8032, and #8034 were merged without bypassing required checks.

### Follow-up

None.
