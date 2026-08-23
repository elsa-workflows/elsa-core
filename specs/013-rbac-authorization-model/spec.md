# Feature Specification: Authorization Model

**Feature Branch**: `013-rbac-authorization-model`

**Created**: 2026-08-23

**Status**: Draft — pending approval

**Tracking**: [#7974](https://github.com/elsa-workflows/elsa-core/issues/7974)

**Input**: A customer request for role-based access control, assessed in [research.md](research.md), reframed against Elsa's existing identity, permission, and multitenancy infrastructure, [domain language](../../CONTEXT.md), and [architecture decisions](../../docs/adr).

## Product Context

Elsa already has most of an authorization system. Roles carry permissions, tokens carry permission claims, and 151 of 160 endpoint files declare a required permission. What it lacks is a *model*: the permission vocabulary is an open set of ad-hoc strings compared by ordinal equality, with no implication, no grouping, and no catalog.

The consequences are concrete. `"*"`, `"read:*"` and `"exec:*"` are literal claim values rather than patterns, so `read:*` grants access only to the twelve endpoints that happen to list it, out of roughly forty read endpoints. `read:workflow-definitions` does not imply `read:workflow-definitions:versions`. Fifty-seven permission strings appear as inline literals across 174 call sites in three competing naming schemes, so a typo silently produces an unreachable endpoint and no user interface can render a sensible role editor. Omitting the declaration fails open. Four parallel permission-checking mechanisms — FastEndpoints permissions, named ASP.NET policies, hand-rolled claim inspection, and SignalR hub checks — leave no single place to audit. (Read-only mode also uses mid-handler authorization calls, but that is a separate axis and stays as it is.)

This feature replaces that vocabulary with a **structured authorization model**: a hierarchical **resource** axis, an open **verb** axis, a module-contributed **permission catalog**, and a single evaluator that every enforcement path routes through.

Both axes are open and string-keyed, because Elsa is a framework that third parties extend and [ADR 0004](../../docs/adr/0004-separate-external-identity-from-elsa-authorization.md) establishes an open permission vocabulary. A closed verb enumeration was drafted and rejected: fitting the census to seven verbs forced six mappings and three invented sub-resources, and every open question it produced was an artefact of the closure. Coherence is maintained by a recommended core verb set as convention, per Principle III. [ADR 0009](../../docs/adr/0009-match-unlinked-identities-with-trusted-user-matchers.md) remains in force: Elsa is the only authority that expands Roles into permission claims.

## Clarifications

### Session 2026-08-23

- The permission vocabulary is a clean break. Canonical form is `{resource}:{verb}`, reversing today's `{verb}:{resource}`. The existing scheme is ad hoc and carries three competing conventions; it is replaced rather than preserved.
- The resource axis is hierarchical with prefix matching. `/` separates depth, `:` separates resource from verb: `workflows/definitions:view`, `workflows/*:view`, `*:*`.
- Wildcards are the only construct with forward reach, on either axis: `workflows/*` covers resources registered later, `definitions:*` covers verbs added later, and `*:*` is superuser without a special case.
- Aggregates are not part of the model and no verb implies another, matching both Elsa's current behavior and the original proposal's own worked example. `manage` is a user-interface preset, not a model concept.
- Descriptors declare which verbs each resource supports. This is required regardless — to render a role editor, validate a submitted grant, and report the current reach of a wildcard grant.
- Storage is unchanged. `Role.Permissions` stays a string collection holding flat `{resource}:{verb}` entries.
- Legacy permission strings stop matching. A startup validator reports them loudly and a migration document carries the full mapping. Operators of the requesting deployment have confirmed they will migrate rather than requiring a compatibility layer.
- Revocation latency is addressed by lowering the default access-token lifetime from 1 hour to 15 minutes. An optional per-user security stamp is available where tighter bounds are required, and must not depend on cross-node cache invalidation, which Elsa does not have.
- All in-repository endpoints migrate at once. `ConfigurePermissions(params string[])` remains obsolete-but-functional so third-party modules keep compiling; an unresolvable third-party permission registers an implicit unverified descriptor and logs a warning rather than failing the host at boot.
- Elsa gains no cross-tenant principal. Administering multiple tenants belongs to applications built above Elsa; machine access uses a tenant-scoped `Application` credential.
- Tenancy hardening for Identity is in scope. Tenancy for `Elsa.Secrets` is not, and is tracked as [#7972](https://github.com/elsa-workflows/elsa-core/issues/7972).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Grant a Whole Section in One Grant (Priority: P1)

An administrator creates a "Workflow Operator" role that may view everything under workflows and start instances, without enumerating individual resources and without that role silently missing endpoints added in a later release.

**Acceptance**: Granting `workflows/*:view` authorizes every current resource beneath `workflows/`, including definitions, instances, activity executions, and all descriptor endpoints. A resource added under `workflows/` in a subsequent release is covered by the same grant with no role edit.

### User Story 2 - Deny by Default, Fail Closed (Priority: P1)

A role holding no grant for a resource is refused, and an endpoint whose author forgot to declare a permission does not silently become public.

**Acceptance**: A caller with no matching grant receives 403. An automated build-time gate fails when an in-repository endpoint declares none of a permission, anonymous access, or authenticated-only access.

### User Story 3 - Discover the Permission Catalog (Priority: P1)

A role editor renders the full set of grantable permissions, grouped for a human, without hard-coding strings that drift from the server.

**Acceptance**: A catalog endpoint returns every registered resource with its display metadata, category, and supported verbs. Every permission declared by an in-repository endpoint resolves to a registered descriptor.

### User Story 4 - Know My Own Permissions (Priority: P1)

A client conditionally renders its interface — hiding sections, disabling actions, showing read-only states — from a single call, without probing endpoints.

**Acceptance**: `GET /identity/me/permissions` returns the caller's effective grants for the current tenant context. Resources the caller cannot access are present with an empty verb list rather than absent, so a client can distinguish "denied" from "unknown".

### User Story 5 - Migrate an Existing Deployment (Priority: P2)

An operator upgrading a deployment with hand-authored roles learns exactly which stored permissions no longer resolve, and what to replace them with, instead of discovering it through user reports.

**Acceptance**: On startup, every unrecognized permission in a stored role is logged with its role name. A migration document maps every legacy permission to its replacement. The administrator role, granted `*`, continues to function throughout so an instance cannot be locked out.

### User Story 6 - Revoke Access Promptly (Priority: P2)

Removing a role from a user takes effect within a bounded, documented window.

**Acceptance**: With default settings, revocation takes effect within the access-token lifetime. With the optional security stamp enabled, it takes effect within the configured stamp cache interval without requiring distributed cache infrastructure.

### User Story 7 - Audit Role Changes (Priority: P2)

A compliance reviewer can reconstruct who changed which role, when, and what the resulting grants were.

**Acceptance**: Role creation, update, and deletion, and user role assignment and removal, each publish a typed security notification carrying the resulting grants.

### User Story 8 - Keep Third-Party Modules Working (Priority: P3)

A module maintained outside this repository continues to function after upgrading, with its gaps visible rather than silent.

**Acceptance**: A third-party module calling the obsolete declaration API compiles and runs. Its unrecognized permissions register implicit descriptors marked unverified, appear in the catalog as such, and log a warning.

### User Story 9 - Isolate Roles Between Tenants (Priority: P3)

Two tenants each define a role named `Admin` without collision, and neither can see the other's roles or users.

**Acceptance**: Role and user names are unique per tenant rather than globally. Listing roles or users returns only the current tenant's records under both the Entity Framework and in-memory stores.

### Edge Cases

- A grant whose resource matches but whose verb does not is refused; a partial match never partially authorizes.
- A role holding no grant for a resource is denied; absence is denial, and there is no stored value meaning "no access".
- A wildcard grant confers access to resources registered after the grant was authored. This is intended; the catalog makes current reach inspectable.
- A *concrete* verb outside a resource's declared set, or a concrete resource with no descriptor, is rejected at role-authoring time. Wildcard segments are validated structurally and are accepted even when they currently match nothing, so a grant against a not-yet-installed module survives.
- A permission string containing a comma is rejected, because the persistence converter joins collections with commas.
- Two tenants holding roles of the same name must not collide, and a role must never resolve across a tenant boundary.
- A caller authenticated by API key resolves grants through the application's roles by the same evaluator as an interactive user.
- An endpoint declaring a resource with no registered descriptor fails the in-repository gate at startup.

## Requirements *(mandatory)*

### Functional Requirements

#### Permission Model

- **FR-001**: A permission MUST be a pair of a hierarchical resource path and a verb.
- **FR-002**: The canonical textual form MUST be `{resource}:{verb}`, with `/` separating resource path segments.
- **FR-003**: The verb axis MUST be open and string-keyed, with verbs declared per resource by the owning module.
- **FR-004**: The resource axis MUST remain open, with resources contributed by modules.
- **FR-005**: A request MUST be authorized when a held grant matches both the required resource and the required verb.
- **FR-006**: Elsa MUST publish a recommended core verb set that modules SHOULD reuse, and MUST NOT prevent a module declaring a verb outside it.
- **FR-007**: Both axes MUST support an exact match and a wildcard: a trailing `*` matching a resource subtree, and `*` matching any verb. Wildcards MUST be the only construct conferring access to resources or verbs registered later.
- **FR-008**: A role holding no matching grant MUST NOT be authorized; absence of a grant is denial.
- **FR-009**: The model MUST NOT define verb aggregates, and no verb may imply another.
- **FR-010**: Effective permissions MUST be the union of grants across all roles held by the principal.

#### Catalog and Descriptors

- **FR-011**: Every module exposing protected endpoints MUST contribute permission descriptors through a registry hosted in core rather than in an optional module.
- **FR-012**: A descriptor MUST declare the resource, its supported verbs, display name, description, and category.
- **FR-012a**: Role create and update MUST reject a concrete resource with no registered descriptor, and a concrete verb outside that resource's supported verbs. Wildcard segments MUST be validated structurally only.
- **FR-013**: The catalog MUST be exposed through an endpoint suitable for driving a role editor, and MUST mark verbs outside the recommended core set.
- **FR-014**: Every permission declared by an in-repository endpoint MUST resolve to a registered descriptor, verified by an automated gate.
- **FR-015**: The catalog MUST be able to report the resources a given wildcard grant currently covers.

#### Enforcement

- **FR-016**: All *permission* decisions MUST route through a single evaluator. Authorization concerns that are not permission checks — notably read-only mode — are a separate axis and MUST retain their own enforcement.
- **FR-017**: The existing hand-rolled permission-claim inspections, named-policy permission checks, and SignalR hub permission checks MUST be replaced by calls to that evaluator. This does NOT extend to the mid-handler `NotReadOnlyPolicy` calls in the workflow API: those enforce deployment read-only mode rather than a permission, and folding them into the permission evaluator would conflate two independent axes.
- **FR-018**: Endpoints MUST declare their requirement as a resource constant plus a verb, with the resource constant shared with the descriptor declaration.
- **FR-019**: Every in-repository endpoint MUST declare exactly one of: a required permission, anonymous access, or authenticated-only access. An endpoint declaring none MUST fail an automated build-time coverage gate. The authenticated-only state exists so that a deliberate "needs an identity but no grant" choice is distinguishable from an author's omission.
- **FR-020**: A failed authorization check MUST return 403.
- **FR-021**: Superuser access MUST be expressed within the model as the whole-vocabulary grant `*:*`, not as a special-cased sentinel.

#### Roles, Grants, and Introspection

- **FR-022**: Roles MUST support create, read, update, and delete, scoped to a tenant.
- **FR-023**: A caller MUST NOT be able to create or modify a role granting permissions the caller does not hold.
- **FR-024**: A caller MUST NOT be able to assign a role granting permissions the caller does not hold.
- **FR-025**: An endpoint MUST return the calling principal's effective grants for the current tenant context.
- **FR-026**: That response MUST include resources the caller cannot access, carrying an empty verb list.
- **FR-027**: Role and assignment mutations MUST publish typed security notifications, without this feature owning audit persistence.

#### Tokens and Revocation

- **FR-028**: Permission claims MUST continue to be issued by Elsa alone, from roles.
- **FR-029**: The default access-token lifetime MUST be 15 minutes, documented as the revocation bound. Refresh MUST continue to rotate both tokens and re-read roles, so that each refresh reflects current grants.
- **FR-030**: An optional per-principal security stamp MUST be available to tighten that window.
- **FR-031**: The security stamp MUST NOT require cross-node cache invalidation or additional infrastructure.

#### Migration and Compatibility

- **FR-032**: Legacy permission strings MUST NOT authorize under the new vocabulary.
- **FR-033**: Startup MUST report every stored permission that does not resolve, identified by role.
- **FR-034**: A migration document MUST map every legacy permission to its replacement.
- **FR-035**: The whole-vocabulary grant MUST survive migration unchanged, so an administrator cannot be locked out.
- **FR-036**: The existing string-based declaration API MUST remain functional but obsolete.
- **FR-037**: An unresolvable third-party permission MUST register an implicit descriptor marked unverified and log a warning, rather than preventing startup.

#### Tenancy

- **FR-038**: Role and user names MUST be unique per tenant rather than globally.
- **FR-039**: The default in-memory user and role stores MUST filter by tenant.
- **FR-040**: Role and user listing MUST filter by tenant explicitly, not solely through an ambient persistence filter.
- **FR-041**: Elsa MUST NOT introduce a principal that spans tenants.

### Key Entities

- **Permission**: a resource path paired with a verb; the unit of both declaration and grant.
- **Verb**: an open, module-declared action name; Elsa publishes a recommended core set as convention.
- **Permission Descriptor**: module-contributed metadata for one resource — supported verbs, display name, description, category.
- **Role**: a tenant-scoped, named collection of permissions.
- **User**: a tenant-scoped principal holding role identifiers.
- **Application**: a tenant-scoped machine principal holding role identifiers, authenticated by API key or client credentials.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A grant of `workflows/*:view` authorizes all resources beneath `workflows/`, where `read:*` authorizes twelve of approximately forty read endpoints today.
- **SC-002**: Every in-repository endpoint declares a permission resolving to a registered descriptor, verified automatically; the current figure is 151 of 160 files declaring, with no descriptor coverage for core permissions.
- **SC-003**: Permission decisions route through one evaluator, replacing four parallel permission-checking mechanisms — FastEndpoints permissions, named policies, hand-rolled claim inspections across fifteen files, and four SignalR hub checks. Read-only mode keeps its own enforcement and is out of scope.
- **SC-004**: A role editor can be built with no hard-coded permission strings.
- **SC-005**: An operator upgrading a deployment with legacy roles receives a complete, actionable startup report and cannot be locked out.
- **SC-006**: Revocation takes effect within a documented bound under default settings, and within a configurable shorter bound with the optional stamp, without new infrastructure.
- **SC-007**: Two tenants can each define a role named `Admin`, which is impossible today.

## Assumptions

- The requesting deployment has hand-authored roles in production and has confirmed it will migrate, so no permanent compatibility layer is required.
- Whether the requesting deployment isolates per Elsa tenant or per Elsa instance is unconfirmed. Tenancy hardening is justified independently as a latent-defect fix and is sequenced last so the answer does not block delivery.
- Product-level section taxonomies belong to applications built above Elsa, expressed as groupings over the resource tree.
- Cross-tenant administration belongs to applications built above Elsa.
- `Elsa.Secrets` tenancy is out of scope and tracked separately as [#7972](https://github.com/elsa-workflows/elsa-core/issues/7972).
- Studio and other clients live outside this repository and consume the catalog and introspection endpoints rather than hard-coded strings.
