# Authorization model: assessment and design record

## Context

A request was raised for a five-layer role-based access control model: Modules → Roles →
Functionalities → Scopes (bitmask) → Role assignments. The proposal was treated as a suggestion; the
task was to capture the underlying requirements and decide what Elsa should actually do.

Two questions were put directly: **does a coarse "module"/domain gate make sense as its own layer**,
and **is a bitmask scope design a good idea**. The short answers, expanded below, are *no — not as a
second runtime gate; the requirement belongs in layers Elsa already has* and *yes in substance, no in
the form proposed*.

**Decisions taken (2026-08-20):**

- The **foundation lands in elsa-core**; product-level section taxonomies and role editors are
  built on top of it by downstream applications.
- **Users remain one-per-tenant.** A person operating in two tenants has two user records. We do
  not adopt `UserRoleAssignment { userId, roleId, tenantId }`, and login gains no tenant selection.
- **We push back on the per-role module toggle** and offer the three homes it already has in Elsa.
- **Scope is a closed flags enum; the resource axis stays open strings.**

---

## 1. What Elsa already has

Far more than the ticket assumes. The endpoint-declaration layer is essentially complete.

| Piece | Where | State |
|---|---|---|
| `Role { Name, ICollection<string> Permissions }` | [Role.cs](src/modules/Elsa.Identity/Entities/Role.cs) | Tenant-scoped via `Entity.TenantId` |
| `User { Name, ICollection<string> Roles }` | [User.cs](src/modules/Elsa.Identity/Entities/User.cs) | Single `TenantId`; roles by ID |
| Roles → permission claims | [DefaultAccessTokenIssuer.cs](src/modules/Elsa.Identity/Services/DefaultAccessTokenIssuer.cs), [DefaultElsaTokenService.cs](src/modules/Elsa.Identity/Services/DefaultElsaTokenService.cs) | Union of role permissions, baked into the JWT as one `permissions` claim each |
| Endpoint declaration | [Endpoints.cs](src/common/Elsa.Api.Common/Abstractions/Endpoints.cs) `ConfigurePermissions` | **151 of 160 endpoint files already declare permissions** |
| Escalation guard | [RoleAuthorizationService.cs](src/modules/Elsa.Identity/Services/RoleAuthorizationService.cs) | Caller may only grant permissions they hold |
| Permission catalog | [ExternalAuthenticationContracts.cs](src/modules/Elsa.ExternalAuthentication/Contracts/ExternalAuthenticationContracts.cs) `IPermissionDescriptorProvider` / `IPermissionDescriptorRegistry`, `PermissionDescriptor(Name, DisplayName, Description, **Category**)` | Exists, but **only one provider is registered** — External Authentication's |
| Deployment grant boundary | `PermissionGrantBoundary` in [DefaultPermissionGrantResolver.cs](src/modules/Elsa.ExternalAuthentication/Services/DefaultPermissionGrantResolver.cs), `PermissionGrantOptions.{Allowed,Denied}Permissions` | Exists, external-auth-scoped |
| Per-tenant capability composition | CShells `IShellFeature`, [ShellInstalledFeatureProvider.cs](src/common/Elsa.Features/Services/ShellInstalledFeatureProvider.cs), `GET /features/installed` | Exists |
| Tenant scoping | `Entity.TenantId`, [SetTenantIdFilter.cs](src/modules/Elsa.Persistence.EFCore.Common/EntityHandlers/SetTenantIdFilter.cs), `""` = default, `"*"` = agnostic (ADR 0009) | Exists for EF Core |
| Richest scope model in the repo | `ConnectionScope(ConnectionScopeKind { Host, DefaultTenant, Tenant }, TenantId)` in [ExternalAuthenticationModels.cs](src/modules/Elsa.ExternalAuthentication/Models/ExternalAuthenticationModels.cs) | The precedent to follow for role scoping |

Governing ADRs: [0004](docs/adr/0004-separate-external-identity-from-elsa-authorization.md) —
Elsa's permission vocabulary is deliberately **open**, composed through grant sources;
[0009](docs/adr/0009-match-unlinked-identities-with-trusted-user-matchers.md) — **Elsa is the
only authority that expands Roles into `permissions` claims**;
[0007](docs/adr/0007-publish-audit-ready-security-notifications.md) — typed security notifications
over `INotificationSender`, no audit persistence in the producing module.

### The real defects in today's model

1. **No matcher.** `"*"`, `"read:*"` and `"exec:*"` are *literal claim values*, not patterns.
   `read:*` works only on the 12 endpoints that happen to list it — out of ~40 read endpoints.
   Granting `read:*` today gives inconsistent, unpredictable coverage. This is the single
   strongest argument for the original proposal.
2. **No implication.** `read:workflow-definitions` does not imply
   `read:workflow-definitions:versions`. Every relationship is hand-enumerated at the call site.
3. **No catalog.** ~56 permission strings are inline literals across 174 call sites, in three
   competing naming schemes (`read:secrets`, `external-authentication:connections:read`,
   `ai:tools:view`), plus outliers (`exec:` vs `execute:`, PascalCase `ManageWorkflowRuntime`).
   A typo silently creates an unreachable endpoint. No UI can render a sensible role editor.
4. **Fails open.** Omitting `ConfigurePermissions` inherits the FastEndpoints default — there is
   no Elsa-level fallback and no startup guard.
5. **Four parallel enforcement mechanisms** with no single choke point: FastEndpoints
   `Permissions()`, ASP.NET policies (3 sites), mid-handler `AuthorizeAsync` (15 sites), and
   hand-rolled claim greps (~10 sites, one of which uses `OrdinalIgnoreCase` while the rest use
   `Ordinal`).
6. **JWT bloat.** One claim per permission. A `"*"`-free admin carries ~57 claims on every request.
7. **Stale grants.** Permissions are baked into the token at login. Revoking a role has no effect
   until the token expires. The ticket assumes per-request resolution; Elsa does not do that.

### Tenancy gaps the ticket assumes away

The ticket's "roles configurable per tenant, never shared" is not currently safe to promise:

- **`User.Name`, `Role.Name`, `Application.{ClientId,Name}` have globally unique indexes, not
  per-tenant composite indexes** ([Configurations.cs](src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs)).
  Two tenants cannot both have a role called `Admin`. This is a hard blocker.
- **Default `IUserStore`/`IRoleStore` are memory-backed and tenancy-blind**
  ([MemoryStore.cs](src/modules/Elsa.Common/Services/MemoryStore.cs)). Isolation exists only on the
  EF Core path, and only when `TenantsOptions.IsEnabled`.
- **`GET /identity/users` and `/identity/roles` pass an empty filter** — they rely entirely on the
  ambient EF global query filter.
- **`UserManager.CreateUserAsync` never sets `TenantId`** — it relies on the EF saving handler.
- **`Elsa.Secrets` is entirely tenancy-blind** — `Secret` is a POCO with no `TenantId`. The ticket
  wants per-tenant secrets access control; the data is not tenant-partitioned at all.
- **`Elsa.Persistence.VNext*` has no tenancy plumbing whatsoever.**
- **There is no host/root administrator concept** in `Elsa.Identity`.

---

## 2. Assessment of the proposal

### Layer 1 — Modules: the requirement is real, the mechanism is wrong

The proposal justifies the module layer with two arguments:

- *"Easy to define broad roles without carefully zeroing out every functionality grant."*
  This only bites if the role editor pre-populates every functionality with a non-zero default.
  With deny-by-default grants, "Dashboards only" is simply *grant Dashboards, grant nothing else*.
  This is a UI problem being solved in the authorization model.
- *"Any future endpoint under a disabled module is automatically blocked."*
  Already true under deny-by-default. A new endpoint declaring `Workflows:View` is blocked for any
  role without a Workflows grant. The module layer adds nothing here.

Against it:

- **`modules` and `grants` are keyed by the same six names.** Two sources of truth for one
  taxonomy, which can and will drift (module on with no grants; grants with module off).
- **Two independent gates make 403s hard to diagnose.** An admin grants `Workflows: Manage`, still
  gets 403, and now has to know to check a second screen.
- The only thing it genuinely adds over grants is a *kill switch that overrides the union* — i.e.
  an explicit deny. Denies over a union of roles are order-dependent and confusing, and are a much
  larger semantic commitment than the ticket acknowledges.

**Recommendation — split the requirement across the two layers Elsa already has:**

| Underlying requirement | Where it belongs in Elsa |
|---|---|
| "This tenant doesn't have Secrets at all" (plan/provisioning) | Shell features — the module isn't installed, so the endpoints don't exist. **404, not 403.** Already works. |
| "This deployment forbids anyone from ever holding X" | `PermissionGrantBoundary` allow/deny lists — promote from `Elsa.ExternalAuthentication` to `Elsa.Identity` |
| "Author a broad role quickly, grouped by section" | `PermissionDescriptor.Category` — the descriptor registry already carries this. The role editor renders section groups with a whole-section toggle that **writes grants**. Same UX, one source of truth, zero runtime redundancy. |

If the requester still insists on a hard per-role gate after seeing this, it should be an explicit
deny set with documented union semantics — and I'd keep it out of v1.

### Layer 2 — Functionality + bitmask scope: right instinct, fix the details

What they have correctly identified is that Elsa's vocabulary lacks **implication** on the verb
axis and **grouping** on the resource axis. Both are genuine defects (see §1). The bitwise check
`(userScope & requiredScope) === requiredScope` is correct and should be kept verbatim.

Concrete problems with the proposed encoding:

- **`Manage = 31` includes an unnamed bit (16).** An aggregate defined as a literal that contains
  an undocumented member is a maintenance trap.
- **`All = 127` spans two unnamed bits (16 and 64).** `All` and `Manage|Settings` (63) differ by a
  bit with no meaning. Aggregates must be *derived from named members*, never written as literals.
- **A stored aggregate freezes at grant time.** Add `Export = 64` later and a role holding `31`
  does not get it. That is the *safe* behaviour and I'd keep it — but it must be an explicit,
  documented decision, because administrators read "Manage" as "everything".
- **`Settings = 32` sits outside `Manage`**, so "Manage" doesn't manage settings. Defensible,
  surprising, must be documented.
- **A fixed global verb set is a closed vocabulary**, which collides head-on with ADR 0004's open
  vocabulary and with Elsa being a framework third parties extend.
- **Elsa's real verbs are not CRUD.** Today's vocabulary includes `publish`, `retract`, `exec`,
  `run`, `cancel`, `replay`, `refresh`, `reload`, `trigger`, `complete`, `rotate`, `revoke`,
  `test`, `use`, `import`, `export`, `ingest`, `approve`, `apply`. Collapsing
  `publish:workflow-definitions` into `Edit` destroys the author-vs-publisher separation
  customers ask for.

**Recommendation — take the good half and split the axes by openness:**

- **Verb/scope axis: closed, platform-defined `[Flags] PermissionScope`.** This is where the
  customer's idea pays off, and a closed set is genuinely appropriate here.
- **Resource/functionality axis: open, string-keyed, contributed by modules** via
  `IPermissionDescriptorProvider`. This preserves ADR 0004 and third-party extensibility.

Canonical permission string stays `{resource}:{scope}` so all 174 existing call sites, the claim
format, and the audit trail keep working. Grants are stored compactly as `(resource, scopeMask)`.
The matcher becomes:

```
granted(resource, mask) satisfies required(resource, requiredMask)
  iff resourceMatches(granted.resource, required.resource)     // '*' + hierarchical prefix
   && (mask & requiredMask) == requiredMask                     // the proposed check, verbatim
```

This fixes the `read:*` defect *by construction*, gives implication ordering for free, and
collapses the admin JWT from ~57 permission claims to roughly one per resource.

### Layer 3/5 — Roles and assignments per tenant

`Role` already has `TenantId`, but "never shared across tenants" conflicts with wanting a
platform-level administrator. Elsa's `"*"` agnostic sentinel and External Authentication's
`ConnectionScope(Host | DefaultTenant | Tenant)` are the right precedent: support host-scoped roles
assignable only by host administrators, plus tenant-scoped roles. Also requires fixing the global
unique indexes on `Role.Name` / `User.Name`.

`UserRoleAssignment { userId, roleId, tenantId }` implies **one person holding different roles in
different tenants**. Elsa today has a single `User.TenantId`, resolves the tenant *from the user*
([CurrentUserTenantResolver.cs](src/modules/Elsa.Identity/Multitenancy/CurrentUserTenantResolver.cs),
[ClaimsTenantResolver.cs](src/modules/Elsa.Identity/Multitenancy/ClaimsTenantResolver.cs)), and has
no tenant selection at login ([Login/Endpoint.cs](src/modules/Elsa.Identity/Endpoints/Login/Endpoint.cs)).

**Decided: we do not adopt this.** Users stay one-per-tenant; a person operating in two tenants has
two user records, and `User.Roles` remains a flat list resolved within the user's own tenant. This
avoids making users tenant-agnostic, adding tenant selection at login, carrying a per-session
tenant in the token, and reworking grant resolution to be per-`(user, tenant)` — by far the largest
structural change the ticket implied, and one carried by a single line in their data model.

What still has to be true for "roles configurable per tenant" to hold is the tenancy hardening in
Phase 3 — the global unique indexes and the tenancy-blind memory stores are real blockers today.

### `GET /me/permissions` — yes, unambiguously

Genuinely missing, cheap, and it unblocks their UI. It should return the resolved grants *plus*
the descriptor catalog *plus* which sections are actually installed (from `IInstalledFeatureProvider`).
That hands them their "modules map" derived from the real capability layer, with no duplicate
toggle to drift out of sync.

### Audit — yes, and there's a pattern to follow

ADR 0007 already established typed, redacted security notifications over `INotificationSender`
with no audit persistence in the producing module. Role and assignment mutations should publish
the same shape.

### The gap the ticket doesn't mention: revocation latency

Permissions live in the JWT. Removing a role does nothing until the token expires. For an RBAC
feature bought for compliance, "I revoked their access and they deleted workflows for another
30 minutes" is a real finding. Fix with a **security stamp**: a monotonic counter on the user,
bumped on any role/grant/membership change, embedded as a claim and validated per request against
a cached value (`Elsa.Caching` already provides distributed invalidation).

---

## 3. Recommended work

### Phase 1 — Foundation (Elsa-wide value, no breaking change)

1. **Promote the permission catalog into core.** Move `IPermissionDescriptorProvider` /
   `IPermissionDescriptorRegistry` / `PermissionDescriptor` from
   `Elsa.ExternalAuthentication` to `Elsa.Api.Common` (or `Elsa.Identity`); leave type-forwarding
   shims. Extend the record to `(Resource, Scope, DisplayName, Description, Category)`.
   Every module with endpoints contributes a provider, following the existing
   [ExternalAuthenticationPermissions.cs](src/modules/Elsa.ExternalAuthentication/Permissions/ExternalAuthenticationPermissions.cs)
   shape. This alone removes `unknown_permission_descriptor` warnings for every core permission.
2. **Add `[Flags] PermissionScope`** in `Elsa.Api.Common`, with aggregates derived from named
   members (`Manage = View | Create | Update | Delete | Execute`), never literals, and no `All`
   constant that spans unnamed bits.
3. **Single `IPermissionEvaluator` + one `IAuthorizationHandler`** replacing FastEndpoints'
   exact-match check. This becomes the one auditable choke point and also serves the SignalR
   hubs, the mid-handler checks and `RoleAuthorizationService`.
4. **`RequirePermission(resource, scope)`** as a typed overload alongside the existing
   `ConfigurePermissions(params string[])` in
   [Endpoints.cs](src/common/Elsa.Api.Common/Abstractions/Endpoints.cs) — incremental migration,
   151 call sites keep compiling. Collapse the six copy-pasted method bodies while there.
5. **Fail closed.** A startup guard (and a test) asserting every discovered `ElsaEndpoint*`
   declares either a permission or `AllowAnonymous`.
6. **`GET /identity/me/permissions`** returning resolved grants + catalog + installed sections.
7. **Normalize the vocabulary** to one `{resource}:{verb}` scheme, with the old strings kept as
   aliases in the descriptor so existing role documents keep working.

### Phase 2 — Structured grants

8. `Role.Grants: ICollection<PermissionGrant(Resource, ScopeMask)>` alongside the existing
   `Permissions` list; dual-read during migration, expand grants → strings at token issuance so
   nothing downstream changes on day one. Note the EF converter joins collections with commas
   ([Configurations.cs](src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs)),
   so grants need their own serialization.
9. Compact the `permissions` claim to one entry per resource.
10. **Security stamp** for revocation (above).
11. Promote `PermissionGrantBoundary` to a deployment-level allow/deny in `Elsa.Identity`.
12. Publish role/assignment audit notifications per ADR 0007.

### Phase 3 — Tenancy hardening (required for "roles per tenant" to be true)

Scoped down by the one-user-per-tenant decision: no cross-tenant assignments, no tenant selection
at login. What remains is closing the gaps that make the current per-tenant promise unsafe.

13. **Per-tenant composite unique indexes** on `User.Name`, `Role.Name`, `Application.ClientId`,
    `Application.Name` in [Configurations.cs](src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs),
    plus migrations across all six EF providers. Today two tenants cannot both have an `Admin` role.
14. **Tenant-aware `MemoryStore`** ([MemoryStore.cs](src/modules/Elsa.Common/Services/MemoryStore.cs))
    so the default `IUserStore`/`IRoleStore` isolate, not just the EF path.
15. **Explicit tenant filtering** in `GET /identity/users` and `/identity/roles` (they pass an empty
    filter today) and in `UserManager.CreateUserAsync` (never sets `TenantId`) — belt and braces
    over the ambient EF query filter, which only applies when `TenantsOptions.IsEnabled`.
16. **Tenant scoping for `Elsa.Secrets`** — `Secret` is a POCO with no `TenantId`. Per-tenant
    secrets access control is unenforceable until the data is partitioned. Sizeable on its own;
    may warrant a separate ticket.
17. **Host-scoped vs tenant-scoped roles**, modelled on `ConnectionScope(Host | DefaultTenant | Tenant)`
    from External Authentication, using the `"*"` agnostic sentinel. Answers "who administers the
    platform", which the proposal's *never shared across tenants* rule leaves no room for.

### Not Elsa's job

The six product sections (Workflows, Instances, Dashboards, Secrets, Connections, User Management)
are a product-level taxonomy, not the platform's — Elsa has ~20 modules with endpoints. They belong in the
downstream application as a **deployment-defined grouping over descriptor categories**, together with the role
editor UI.

---

## Verification

- Unit: `PermissionScope` aggregate derivation; `IPermissionEvaluator` matcher table
  (`*`, `read:*`, hierarchical prefixes, mask containment, `scope: 0` denies everything) —
  alongside the existing [RoleAuthorizationServiceTests.cs](test/unit/Elsa.Identity.UnitTests/Services/RoleAuthorizationServiceTests.cs).
- Guard test: enumerate every `ElsaEndpoint*` type and assert a declared permission or
  `AllowAnonymous`; assert every declared permission resolves to a registered descriptor.
- Integration: `GET /identity/me/permissions` for a role holding partial scopes; a revoked role
  denying on the next request once the security stamp lands.
- Regression: existing role documents with legacy permission strings still authorize
  ([DefaultAccessTokenIssuerRegistrationTests.cs](test/unit/Elsa.Identity.UnitTests/Services/DefaultAccessTokenIssuerRegistrationTests.cs),
  [LegacyIdentityEndpointTests.cs](test/integration/Elsa.ExternalAuthentication.IntegrationTests/Compatibility/LegacyIdentityEndpointTests.cs)).
- Multi-tenant: two tenants each with a role named `Admin` (fails today); a role listed in tenant A
  is invisible to tenant B against both the EF and memory stores.
- Record the outcome as an ADR and a `specs/0NN-authorization-model/` spec, matching the
  repo's spec-driven convention.

---

## Decisions log (grilling session, 2026-08-23)

Supersedes anything above that conflicts.

**D1 — Clean break on the vocabulary.** Canonical form is `{resource}:{verb}`, reversing today's
`{verb}:{resource}`. Today's scheme is ad hoc and carries three competing conventions; we design
the ideal model rather than preserve it.

**D2 — Upgrade is a loud, one-time migration, not a permanent alias layer.** Legacy strings stop
matching. A startup validator scans stored roles and logs every unrecognized permission by role
name; `docs/migrations/authorization-model.md` carries the full old→new table, following the
convention of `docs/migrations/external-authentication-persistence.md`. Fails closed, loudly.
`*` survives as the escape hatch (see D3), so the admin role cannot be locked out.
*Resolved 2026-08-23:* the requesting deployment does have hand-authored roles in production and accepts migrating, so no compatibility layer is required.

**D3 — `All = ~0u`, not a finite literal, and not a wrapper type.** Forward-widening falls out of
the arithmetic: `All & <future bit> == <future bit>` passes, while a named aggregate like
`Manage = View|Create|Update|Delete|Execute` stays frozen. One integer, both behaviours, no
sentinel and no discriminated union. The proposal's `All = 127` is the right idea with the wrong
literal. Superuser `*` stops being special-cased entirely — it is just `("*", All)`, collapsing the
15 hand-rolled `PermissionNames.All` sites into one evaluator call.

**D4 — Named aggregates are authoring macros, expanded server-side on write.** `manage` never
reaches storage; the create/update role endpoint expands it into its constituent verbs. This
removes the freeze-vs-widen ambiguity from the stored model entirely and makes a role document
say exactly what was granted. The descriptor registry owns the macro table so it is discoverable
via the catalog endpoint rather than reimplemented per client. "I clicked Manage" is recovered in
the ADR-0007 audit notification, not by weakening storage.

**D5 — Storage does not change.** *(The mask referenced below is withdrawn by D13; grants are matched as strings.)* `Role.Permissions` stays `ICollection<string>` holding flat
`{resource}:{verb}` entries. The scope mask is a runtime representation computed by grouping
parsed entries per resource. Note the EF converter joins with commas
(`string.Join(",", v)` in `Modules/Identity/Configurations.cs`), so no permission string may ever
contain a comma — which rules out storing compound masks as text in that column.

**D6 — The resource axis is hierarchical, with prefix matching.** `/` separates depth, `:` splits
resource from verb: `workflows/definitions:view`, `workflows/*:view`, `*:*`. The hierarchy is
already latent in the endpoint layout and in the current vocabulary's inconsistent gestures at it.

### Consequence: the module-layer rebuttal changes shape

D6 is what makes the pushback honest. On a flat axis, "grant the whole Workflows section" is ~15
hand-enumerated grants that silently miss anything added later — which is exactly the pain behind
the proposed module layer, and that fix would have been reasonable. With prefix matching,
`workflows/*:view` is one grant covering definitions, instances, executions and all ten descriptor
endpoints, including future ones.

So the answer to the request is no longer "you don't need that." It is: **you get that natively,
in a single grant, on an axis that composes with scope instead of overriding it.** Their
`Workflows: View` becomes `workflows/*:view` — same expressiveness, one source of truth, and a 403
explainable from a single grant list.

Accepted cost: prefix matching gives grants implicit forward reach on the resource axis, the same
double-edge as the verb wildcard. Mitigation is inspectability — the descriptor catalog lets
the role editor show "this grant currently covers these 15 resources."

### Corrections to the assessment above

- **JWT bloat was overstated.** The admin case is a single `*:*` claim, not ~57. Compaction is an
  optimization, not a design driver, and should not be sold as one.
- **The proposed `/me/permissions` shape survives unchanged.** The runtime mask projects to an
  integer as they specified. One wrinkle to tell them: `All` projects as `-1` / `4294967295`, which
  is correct under their JS check (32-bit signed: `-1 & 8 === 8`) but looks alarming.
- **Phase 1 item 7 is smaller than written** — declare canonical forms in descriptors; the rewrite
  is handled by D2's migration, not by an alias layer.

**D7 — Revocation: shorten token lifetime by default; security stamp is opt-in.** `AccessTokenLifetime`
defaults to 1 hour, so a revoked role currently stays live that long. Default guidance becomes a
short access-token lifetime (the refresh endpoint already exists); an optional per-user security
stamp, cached per node under a short TTL, is available where tighter bounds are required.
*Correction:* the assessment above claimed `Elsa.Caching` provides distributed invalidation. It does
not — `ChangeTokenSignalInvoker` is a per-process `ConcurrentDictionary`, and the only cross-node
primitive in the repo is a distributed *lock* (`IDistributedLockProvider`), not an invalidation
broadcast. The stamp design therefore must not depend on cross-node invalidation, and must not
make Redis a prerequisite for correct RBAC.

**D8 — Endpoint declaration follows the External Authentication pattern, with one refinement.**
Constants and descriptors colocated per module (as in `Permissions/ExternalAuthenticationPermissions.cs`),
endpoints reference the constants. Refinement: **one constant per *resource*, not per resource+verb
pair** — `N` constants instead of `N×M`, with the verb supplied by the enum and therefore
type-checked rather than buried in an opaque string.

**D9 — Big-bang rewrite in-repo, graceful degradation for third parties.** All 151 endpoint files
migrate at once, split one PR per module, guarded by a startup test asserting every declared
resource resolves to a registered descriptor. `ConfigurePermissions(params string[])` stays
`[Obsolete]` but functional so third-party modules keep compiling. An unresolvable third-party
string auto-registers an implicit descriptor, logs a warning, and is marked unverified in the
catalog — following the existing `unknown_permission_descriptor` precedent in
`DefaultPermissionGrantResolver` rather than failing the host at boot. The fail-closed guard
therefore applies to in-repo endpoints only; that asymmetry is deliberate.

**D10 — No host-scoped principal; the cross-tenant persona is out of scope.** The ticket's "instance
administrator" is a persona of a higher-level application that manages Elsa
instances (comparable to Valence Control), so cross-tenant administration is solved above Elsa.
Elsa does not manage other Elsa instances and will not grow a host principal to imply that it does.
Machine-to-machine access needs nothing new: `Application` already carries `TenantId` and `Roles`,
and `DefaultApiKeyProvider` expands those into permission claims.
*Note:* a user with `TenantId = "*"` is currently unrepresentable — `"*"` is reserved (ADR 0009), so
`CurrentUserTenantResolver` yields an ID absent from the tenant dictionary and
`DefaultTenantResolverPipelineInvoker` logs "could not be found in the tenant store" and returns
null. Recorded because it is non-obvious, not because we intend to change it.

**D11 — Harden tenancy anyway; defer secrets tenancy to its own issue.** Whether a deployment isolates
per Elsa *tenant* or per Elsa *instance* is unconfirmed, but the Phase 3 hardening is justified on
its own merits: globally-unique `Role.Name` / `User.Name` indexes are a latent bug for any
multi-tenant deployment, and the default memory-backed `IUserStore`/`IRoleStore` do no tenant
filtering at all. Sequenced last. `Elsa.Secrets` tenancy is excluded — `Secret` is a POCO with no
`TenantId`, making it a genuine feature rather than a hardening fix; filed separately as elsa-workflows/elsa-core#7972.

**D12 — No macros. Supersedes D4.** Since D4 established that aggregates expand before storage, a
macro has zero runtime semantics: stored grants, evaluation and audit are identical whether the
server expanded `manage` or the client sent the verbs. It is a UI affordance, and implementing it
server-side would commit Elsa to defending a per-resource editorial taxonomy across every module
author, forever. Separately, the proposed `Manage` existed to compensate for a flat resource
axis — pressure that D6 already removed, since `workflows/*` collapses 15 resources into one grant.

Descriptors still declare the verbs each resource supports; that is needed regardless, to render a
role editor, validate submitted grants, and power the "this grant covers these 15 resources"
inspection D6 relies on. What is *not* added is a second field classifying verbs as operational.

Final model: **hierarchical resource + explicit verbs + `*` as the only thing with forward reach.**
Three concepts, no taxonomy to defend.

*Residual risk:* scripted role provisioning (Terraform, CI, curl) is more verbose without
`"scope": "manage"`. Judged insufficient to reintroduce macros — such clients can read the catalog
endpoint — but this is the argument that will return if role setup is ever automated.

---

## D13 — The verb axis is open strings, not a closed enum. Supersedes D3.

**Challenge raised 2026-08-23:** does a closed scope enumeration make sense in a modular, extensible
system, when a module may introduce endpoints for which none of the defined scopes fit?

It does not, and the evidence was already in the draft vocabulary. Fitting the 150-endpoint census
to seven verbs required *adding* `Publish` and `Operate` because publish/retract and
cancel/replay/pause/reload do not fit CRUD; forcing `test` into `Execute`, `rotate` and `revoke`
into `Update`, `ingest` into `Create`, `archive` into `Delete`; collapsing `approve` and `apply`
into one verb; and inventing three sub-resources (`secrets/values`,
`external-authentication/provider-trust`, `.../permission-grants/unrestricted`) purely to express
what the enum could not. Every one of the five "editorial judgements needing module-owner review"
was the same artefact. That is 16 first-party modules in one repository; a third party with
`sign`, `escalate`, `acknowledge` or `quarantine` has nowhere to put them.

**The decisive point is that the mask was not buying what D3 claimed.** The closed enum was
justified on implication — an aggregate covering narrower verbs. D12 removed aggregates, and
without them no verb implies another, in this model or the proposal's. Its own worked example
states it: `hasAccess(2, 8) // false — Edit does not cover View`. So the bitwise AND never
expressed "broader covers narrower"; it expressed "a grant may carry several verbs and a
requirement may need several." That is set containment, which a set of strings satisfies
identically.

With implication gone, the enum's remaining benefits were compactness — already surrendered by D5,
which stores one string per resource-verb pair — and resembling the original proposal.

### The model

```
permission := {resource}:{verb}
resource   := hierarchical, '/'-separated; '*' matches a subtree
verb       := flat string; '*' matches any verb
satisfies  := resourceMatches(granted, required) && verbMatches(granted, required)
```

One matching rule shape on both axes. No enumeration, no mask, no `~0u`, no arithmetic. `*:*`
remains superuser and `workflows/*:*` still widens forward, now by the same wildcard rule that
governs resources rather than by a separate numeric convention.

D12 stands unchanged: no aggregates. `manage` may appear as a *verb* where a module genuinely has
one coarse permission, which is a name rather than an expansion of other verbs.

### Accepted cost: vocabulary fragmentation

Without a closed set, modules will coin `read`/`view`/`get`/`list` for the same idea. Mitigated by
convention rather than enforcement, consistent with Principle III: Elsa ships a **recommended core
verb set** — `view`, `create`, `update`, `delete`, `execute` — as constants that modules should
reuse, and the catalog marks non-core verbs so they surface for review. This is exactly how the
resource axis already works.

### Consequences

- The five open editorial decisions dissolve into plain names: `ai/proposals:approve`,
  `ai/proposals:apply`, `workflows/tasks:complete`,
  `external-authentication/connections:archive`, `secrets:use`. No invented sub-resources.
- `GET /identity/me/permissions` returns `verbs: ["view","publish"]` rather than `scope: 33`. This
  deviates from the proposed integer contract, and is an improvement: that contract was about to
  carry `4294967295` for an administrator, previously flagged as correct but alarming.
- The proposed bitwise check is preserved semantically as set containment.

**D14 — `secrets:use` and the AI proposal verbs stay out of the published vocabulary.** Neither
guards any endpoint today: no endpoint returns a secret value (resolution happens at workflow
runtime through `ISecretResolver`), and `ai:proposals:*` plus `ai:tools:manage` are referenced
nowhere outside their own declaration. The vocabulary describes what exists. Under D13 both are
trivially re-addable as plain verbs when their endpoints ship, with no structural change — which is
precisely why forward-declaring them now buys nothing.

---

## Vocabulary review, 2026-08-23

**D15 — Three root groupings kept: `workflows/`, `identity/`, `system/`.** `workflows/*` is the single
grant that replaces the proposed Workflows module toggle, which is the whole reason the hierarchy
earns its keep. *The scripting caveat originally recorded here is withdrawn by D21, which removes the
scripting execute resources entirely.*

**D16 — Synonym drift corrected in the first draft.** `alterations:run` became `execute`; both uses of
`manage` (`identity-links`, `policies`) became `update`. Both were introduced by the model author
inside a single document, which is direct evidence that the core-verb convention needs active policing
rather than documentation alone. Verified as genuinely distinct and retained: `refresh` (targeted, takes
definition IDs) versus `reload` (wholesale); `revoke` versus `delete` on sessions; `archive` versus
`delete` on connections.

**D17 — `write` replaces `update` where an API does not separate create from update.** A resource
declares **either** `create` + `update` **or** `write`, never both. `update` was rejected for upsert
endpoints because it misdescribes the grant: `POST /workflow-definitions` creates when no definition ID
is supplied, so a role holding "update" could create records. `upsert` was rejected as mechanism-named
jargon where the grant should express intent. Elsa is already bimodal along exactly this line —
`labels`, `identity/users`, `identity/roles`, `external-authentication/connections` separate the
operations; `workflows/definitions`, `workflows/instances`, `secrets`, `tenants` do not — so the
vocabulary reflects the API shape rather than imposing uniformity on it. Never-both is what prevents
`write` becoming an aggregate and keeps FR-009 true.

**D18 — `policies/default-roles` split out; `delegate`/`delegate-unrestricted` stay verbs.** The
default-role list is a distinct thing being administered — the roles granted to a user auto-created for
an unknown external identity — so it earns a resource. "Unrestricted" is a *mode* of one action on one
configuration, not a thing, so it stays a verb. The tier relationship (`mayDelegate = unrestricted ||
hasDelegate`) lives in `DefaultPermissionDelegationAuthorizer` and is deliberately not modelled;
FR-009's absence of verb implication is correct, not a gap.
*Finding, not caused by this work:* the `roles:assign` descriptor overstates its reach. Setting
`defaultRoleIds` is guarded by `RoleAuthorizationService.CanAssignRolesAsync`, the ordinary
subset-of-your-own-permissions rule; `roles:assign` itself is enforced only when removing policy
references during role deletion. The anti-escalation rule holds, so this is not a hole, but the
descriptor should be corrected and the module owner asked whether the gap is intentional.

**D19 — Default access-token lifetime drops from 1 hour to 15 minutes.** `RefreshAsync` calls
`IssueTokensAsync`, which re-reads roles and issues both a new access and a new refresh token, so
refresh already rotates and each refresh reflects current grants — the access-token lifetime *is* the
revocation bound. Cost is roughly four refreshes per user per hour, two store reads each. Refresh-token
lifetime is unchanged at 2 hours; altering session length is a separate UX decision.

**D20 — No Milestone 3 migration scaffold. Supersedes the shim in Complexity Tracking.** The two
compatibility mechanisms operate on different sides and together cover the migration window: the
permanent obsolete `ConfigurePermissions(string[])` path translates legacy *endpoint declarations*
through the migration table, while a legacy *stored grant* still fails to match, which is the intended
break. With the seeded admin holding `*`, module pull requests can land in any order without trunk
regressing. Keeping the evaluator ignorant of legacy strings also protects the component we most want
clean as the single auditable choke point.


**D21 — The C#/Python expression permissions are dropped, not translated.** Challenge raised: if a
principal may execute a workflow, everything that workflow does is permitted, so does an "execute
script" resource make sense at all?

It does not, and inspecting `WorkflowDefinitionScriptAuthorizationService` showed the permission was
conflating two concerns across three enforcement sites. On the **execution** path (`Execute`,
`Dispatch`, `BulkDispatch`) the gate was incoherent: a workflow runs under the server's authority, not
the caller's, so the check never constrained what a script could do — it only decided whether this
caller could trigger a definition someone else authored. `workflows/definitions:execute` already
answers that, and the gate failed badly: an author adding a C# expression silently revoked an
operator's ability to run a workflow they had been running for months. On the **authoring** path
(`Post`, `Import`, `ImportFiles`, `Publish`, `BulkPublish`) the gate was meaningful — saving a C#
expression means running arbitrary host code — but it is a constraint on the *write* path, not an
`execute` verb on a scripting resource, so the name mis-describes it. A third site,
`Endpoints/Scripting/ExpressionDescriptors/List`, uses the same permissions to filter which expression
types the editor offers.

Relocating the authoring gate under `workflows/definitions` would have moved the sharp edge from
`execute` to `write` rather than removing it, so a mis-scoped permission is not carried into the new
vocabulary. Both permissions are removed; per-author script trust is redesigned in
[#7975](https://github.com/elsa-workflows/elsa-core/issues/7975).

**Consequence, which is a deliberate reduction in control and must be prominent in the migration
document:** the host switch (`AllowHostCodeExecution`, surfaced as `IsBrowsable`) becomes the single
control. Where host code is disabled nothing changes. Where it is enabled, any author who may write
definitions may use C# and Python, and the editor offers those types to every such author. Deployments
that enabled host code while permitting only some authors to use it lose that granularity until #7975
lands.

`workflows/scripting/javascript:view` is retained — it serves editor type definitions and is read-only.

**D22 — A subtree wildcard matches the named node and all descendants.** `workflows/definitions/*`
covers `workflows/definitions` itself as well as `.../versions` and `.../labels`. This was unspecified
until the module-owner review exposed it. Inclusive matching is how an administrator reads "grant this
subtree", and it behaves consistently whether or not the parent is itself a registered resource;
withholding a parent while granting children remains possible by naming the children. The alternative —
descendants only — would make `workflows/definitions/*:view` grant versions and labels while denying the
definitions list, which would be reported as a bug.

**D23 — Module-owner review findings (2026-08-23).**

*Census correction.* External Authentication was under-sampled: the original extraction took one route
per file, but that module packs up to eight endpoint classes into a single file, so only 6 of 34
endpoints were seen. A full re-census confirmed no other module does this. The tree survived, with the
corrections below.

*Corrections applied.*
- `external-authentication/identity-links` was `view, update`; `links:manage` actually covers list,
  create, replace and delete, so it becomes `view, write, delete`.
- `ingest:diagnostics:opentelemetry` dropped as declared-but-unused, consistent with D14. Missed in the
  first pass.
- The migration mapping is separated from the resource tree and carries full legacy strings, one row per
  permission. The earlier compressed notation (`` `read:`/`write:`/`delete:workflow-definitions` ``) was
  neither mechanically checkable nor unambiguous for whoever writes the migration document; a
  completeness script against it produced 24 false positives. All 57 literal permissions now verify.
- `read:*` and `exec:*` had no mapping at all despite being real grants in customer role documents. They
  become `*:view` and `*:execute` — which makes them materially *more* powerful, since today they
  authorize only the twelve and one endpoints that happen to list them. Operators must review any role
  holding them.

*Confirmed correct, recorded so it is not "fixed" later.* External Authentication connections have no
hard delete: `DELETE /connections/{connectionId}` maps to the archive permission and pairs with
`restore`, while enable and disable map to update. The absence of `delete` on that resource is
deliberate.

*Migration expands, it does not rename.* Several new sub-resources are granularity increases, so a
single legacy permission maps to multiple new ones — `read:workflow-definitions` becomes both
`workflows/definitions:view` and `workflows/definitions/versions:view`; `links:manage` becomes three
verbs. A migration that substitutes one-for-one will silently narrow existing roles.

*Code finding.* `Broker/Logout.cs` declares two endpoint classes, `Logout` and `ContinueLogout`, neither
of which declares a permission or `AllowAnonymous`; both inherit FastEndpoints' authenticated-without-
permission default. Both will fail the Milestone 3 fail-closed gate and need an explicit declaration.

*Five questions remain for module owners*, recorded at the end of the vocabulary contract: the
workflow-definition upsert verb, whether the six External Authentication descriptor endpoints deserve
their own resource, whether `/user-options` being guarded by `links:manage` is intentional, the
misleading `roles:assign` descriptor, and the `Logout` declarations.

**D24 — Read-only mode is not a permission and keeps its own enforcement. Corrects the assessment
above.** The original defect list counted four parallel enforcement mechanisms, one of them the 15
mid-handler `AuthorizeAsync(..., NotReadOnlyPolicy)` calls in the workflow API, and Phase 1 proposed
that the single evaluator serve "the mid-handler checks" among others. That was wrong.

`NotReadOnlyPolicy` enforces deployment read-only mode — whether this instance accepts mutations at
all — which is orthogonal to whether a principal holds a permission. A workflow author with full
grants is still refused while the deployment is read-only, and correctly so. Folding it into the
permission evaluator would conflate two independent axes and make read-only mode expressible as a
grant, which it must not be.

The consolidation therefore covers four *permission-checking* mechanisms: FastEndpoints permissions,
named ASP.NET policies (3 sites), hand-rolled claim inspections (15 files), and SignalR hub checks
(4 hubs). The count is unchanged; the composition is not. FR-016 and FR-017 are worded accordingly,
and `tasks.md` T040 carries the exclusion explicitly so no one implements it from the task list alone.

**D25 — Module-owner review outcomes (2026-08-23).** Five questions resolved; full text at the end of
`contracts/permissions.md`.

- **EA descriptors get their own resource**, `external-authentication/descriptors:view`, as a single
  node rather than six. One legacy permission governs all six, which is the same principle that gives
  `workflows/descriptors/*` nine separate resources — those were separately permissioned already. The
  tree reflects the API in both cases.
- **`/user-options` stays on `identity-links:view`.** It is a user search backing the link picker,
  returning id and display name scoped to the tenant. *Recorded consequence:* identity-link rights
  therefore confer tenant-wide user enumeration in that reduced projection, without
  `identity/users:view`. Moving it would either over-grant full user read on migration or break linking.
- **`roles:assign` descriptor corrected** to describe what it guards — removing policy references
  during role deletion. No escalation was possible either way, since the subset rule holds regardless.
  Whether it *should* additionally guard `defaultRoleIds` is filed as #7977 rather than settled
  inside a vocabulary migration.
- **The two Logout endpoints declare differently.** `Logout` is authenticated-only; `ContinueLogout` is
  anonymous. **`ContinueLogout` inheriting the authenticated default is a probable live bug** — the
  identity provider redirects the browser there during upstream logout, potentially after the Elsa
  session is gone, so the inherited requirement can 401 a callback that should succeed. Filed as
  #7976. The fail-closed gate surfaced it; it was not introduced by this work.
- **T028 splits four ways** along resource-group seams (31 / 20 / 15 / 12 files) rather than landing as
  one 78-file pull request — the same unreviewable-diff problem the dropped Milestone 3 shim was
  invented to avoid, in a different form.

**D26 — Two model gaps surfaced by D25, one closed and one recorded.**

*Closed:* FR-019 now accepts a **third declaration state**, authenticated-only. `Logout` needs an
identity but no grant, which the original two-state rule could not express — it would have forced
either a fabricated permission or a gate exemption, and an exemption list is a hole in a fail-closed
guarantee.

*Recorded, not built:* **conjunctive requirements are not expressible.** An endpoint declares one
resource and one verb, so "needs link rights *and* user read" cannot be stated declaratively.
`/user-options` is the first case to want it, and `ExternalAuthenticationRoleDeletionDependencyContributor`
already does it imperatively across three permissions. Not needed for this work; recorded so the next
case is not solved ad hoc.
