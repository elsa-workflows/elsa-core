# Implementation Plan: Authorization Model

**Status**: Draft — pending approval

**Tracking**: [#7974](https://github.com/elsa-workflows/elsa-core/issues/7974)

**Branch**: `013-rbac-authorization-model` | **Date**: 2026-08-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/013-rbac-authorization-model/spec.md`, grounded in [research.md](research.md)

## Summary

Replace Elsa's ad-hoc permission vocabulary with a two-axis authorization model: a hierarchical **resource** axis and an open **verb** axis, both module-contributed, evaluated by a single evaluator that every enforcement path routes through.

Both axes are open because Elsa is a framework third parties extend, and because [ADR 0004](../../docs/adr/0004-separate-external-identity-from-elsa-authorization.md) commits to an open vocabulary. Prefix matching on the resource axis makes section-wide grants a single token, which removes the pressure for a second, coarse-grained gate. Wildcards are the only construct with forward reach on either axis, so `*:*` is superuser with no sentinel and no aggregate to reinterpret. A closed verb enumeration was drafted and rejected; see [research.md](research.md) D13.

Storage is unchanged: `Role.Permissions` remains a string collection of flat `{resource}:{verb}` entries.

## Technical Context

**Language/Version**: C# latest; nullable reference types and implicit usings; multi-target `net8.0`, `net9.0`, `net10.0`.

**Primary Dependencies**: `Elsa.Api.Common` (FastEndpoints base classes, permission constants), `Elsa.Identity` (roles, users, applications, token issuance), `Elsa.Features` / CShells shell features, ASP.NET Core authorization, `Elsa.Mediator` for audit notifications, Entity Framework Core for Identity persistence.

**Storage**: No schema change for grants. `Role.Permissions` stays `ICollection<string>` persisted through the existing comma-joining converter in `src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs`, which forbids commas inside a permission string. The tenancy milestone changes Identity indexes only and requires migrations across all five providers.

**Testing**: xUnit unit tests for the resource and verb matchers; a reflection-driven gate asserting every in-repository endpoint declares a permission resolving to a registered descriptor; integration tests for introspection, revocation, and per-tenant isolation; regression tests proving legacy strings no longer authorize and that the whole-vocabulary grant still does.

**Target Platform**: ASP.NET Core Elsa Server; consumed by Elsa Studio and other clients through the catalog and introspection endpoints.

**Project Type**: Modular .NET server libraries with REST endpoints.

**Performance Goals**: Evaluation is O(number of grants held) with no store access on the request path; no measurable regression against today's ordinal set lookup. The catalog is built once per shell and cached.

**Constraints**: No new infrastructure may become a prerequisite for correct authorization — in particular the optional security stamp must not depend on cross-node cache invalidation, which Elsa does not have (`ChangeTokenSignalInvoker` is per-process). Elsa remains the only authority expanding roles into permission claims ([ADR 0009](../../docs/adr/0009-match-unlinked-identities-with-trusted-user-matchers.md)). Permission strings may not contain commas. No cross-tenant principal is introduced.

**Scale/Scope**: Approximately 45 resources rising as modules contribute descriptors; 160 endpoint files; 174 declaration call sites; hand-rolled claim inspections across 15 files; 3 named-policy usages; 4 SignalR hubs. A further 15 mid-handler `NotReadOnlyPolicy` calls exist but are out of scope — read-only mode is a separate axis.

## Constitution Check

*GATE: PASS before research and after design.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| I. Modular Architecture | PASS | The model and evaluator live in `Elsa.Api.Common`; each module owns its own descriptors and constants. The descriptor registry moves out of an optional module into core, correcting an existing inversion. |
| II. Composition & Extensibility | PASS | The resource axis is open and contributed through `IPermissionDescriptorProvider`. Third-party modules keep working through an obsolete-but-functional declaration API with graceful degradation. |
| III. Convention-Driven Design | PASS | Adopts the established `Permissions/<Module>Permissions.cs` pattern already proven in External Authentication, refined to one constant per resource. Verb coherence is maintained by a recommended core set as convention rather than by enforcement. |
| IV. Async & Pipeline Execution | PASS | Evaluation is synchronous and allocation-light by design; catalog contribution and introspection follow existing async contracts. |
| V. Testing Discipline | PASS | Unit, integration, regression, and an automated coverage gate; the gate is itself a deliverable. |
| VI. Trunk-Based Development | PASS | Milestones are independently shippable; the cutover is split one pull request per module, landing in any order because the obsolete declaration path and the seeded admin grant keep trunk green throughout. |
| VII. Simplicity, SRP, DRY & KISS | PASS | Two axes, one matching rule shape, one wildcard. No enumeration, no mask, no aggregates, no second gate, no sentinel. Collapses four parallel permission-checking mechanisms into one and six duplicated method bodies into one; read-only mode correctly keeps its own axis. |

## Project Structure

### Documentation

```text
specs/013-rbac-authorization-model/
├── spec.md          # feature specification
├── plan.md          # this file
├── research.md      # grounded assessment and decisions log
└── contracts/
    ├── rest-api.md      # catalog and introspection contracts
    └── permissions.md   # the resource tree and supported verbs
docs/
├── adr/00NN-two-axis-authorization-model.md
└── migrations/authorization-model.md
```

### Elsa Core Repository

```text
src/common/Elsa.Api.Common/
├── Authorization/
│   ├── CoreVerbs.cs                    # recommended verb constants (convention)
│   ├── Permission.cs                   # (resource, verb) with parse/format
│   ├── IPermissionEvaluator.cs
│   ├── PermissionEvaluator.cs          # the single decision point
│   ├── PermissionMatcher.cs            # exact and wildcard, on both axes
│   ├── PermissionRequirement.cs
│   └── PermissionAuthorizationHandler.cs
├── Permissions/
│   ├── PermissionDescriptor.cs         # promoted from Elsa.ExternalAuthentication
│   ├── IPermissionDescriptorProvider.cs
│   ├── IPermissionDescriptorRegistry.cs
│   └── DefaultPermissionDescriptorRegistry.cs
├── Abstractions/Endpoints.cs           # RequirePermission(resource, scope); collapse 6 duplicates
├── PermissionNames.cs                  # reduced to claim type and the whole-vocabulary grant
└── EndpointSecurityOptions.cs          # remove dead role-name fields

src/modules/<Module>/Permissions/<Module>Permissions.cs   # constants + descriptors, per module
src/modules/Elsa.Identity/
├── Endpoints/Me/Permissions/Endpoint.cs                  # introspection
├── Services/DefaultAccessTokenIssuer.cs                  # emit new-format claims
├── Services/RoleAuthorizationService.cs                  # delegate to the evaluator
└── Options/IdentityTokenOptions.cs                       # shorter default lifetime; stamp options

src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs   # per-tenant indexes
src/modules/Elsa.Common/Services/MemoryStore.cs                          # tenant filtering
```

## Phase 0: Research

Complete. See [research.md](research.md) for the grounded assessment and the twelve decisions (D1–D12) that this plan implements. Two findings materially shaped the design and are recorded there rather than restated: the resource axis had to become hierarchical for the model to remove the need for a coarse second gate, and `Elsa.Caching` provides no distributed invalidation, which constrains the revocation design.

## Phase 1: Data Model and Contracts

Produce `contracts/permissions.md` — the full resource tree with supported verbs per resource, derived from the current 33 resources and the endpoint census — and `contracts/rest-api.md` for the catalog and introspection endpoints. Publish the legacy-to-new mapping as `docs/migrations/authorization-model.md`, following the shape of `docs/migrations/external-authentication-persistence.md`. Record the model in an ADR.

The resource tree is the highest-value artefact to review early: it is the vocabulary every module and client will hold, and it is expensive to change once published.

## Implementation Sequence

### Milestone 1: Model and Evaluator

Additive only; nothing changes behaviour.

- `CoreVerbs`, `Permission`, `PermissionMatcher`, `IPermissionEvaluator` and its implementation.
- Promote the descriptor registry from `Elsa.ExternalAuthentication` into `Elsa.Api.Common`, leaving type-forwarding shims so External Authentication keeps compiling.
- Unit tests for the matcher table: exact match on both axes, subtree wildcard, verb wildcard, whole-vocabulary, absence denying, and a wildcard covering a newly registered resource or verb.

### Milestone 2: Catalog Coverage

Still additive.

- Every module with protected endpoints contributes a `Permissions/<Module>Permissions.cs` carrying one constant per resource and its descriptors, following the External Authentication pattern.
- The catalog endpoint, and the reach report backing "this grant currently covers these resources".
- External Authentication's existing `unknown_permission_descriptor` warning becomes meaningful for core permissions for the first time.

### Milestone 3: Cutover

The breaking change. One pull request per module.

- Endpoints migrate to `RequirePermission(resource, verb)`.
- The hand-rolled claim inspections (15 files), 3 named-policy usages, and 4 SignalR hub checks all route through the evaluator. The 15 mid-handler `NotReadOnlyPolicy` calls are deliberately excluded — read-only mode is a separate axis.
- `ConfigurePermissions(params string[])` becomes obsolete but functional, with unresolvable strings registering implicit unverified descriptors and logging warnings.
- The fail-closed gate lands, asserting every in-repository endpoint declares a permission resolving to a registered descriptor.
- The token issuer emits new-format claims; the startup validator reports unresolvable stored permissions by role.
- No migration scaffold is required: the obsolete `ConfigurePermissions(string[])` path translates legacy endpoint declarations through the migration table, and the seeded admin `*` grant satisfies every endpoint throughout, so module PRs can land in any order. Module-specific authorization fixtures migrate with their module.

### Milestone 4: Introspection, Revocation, and Audit

- `GET /identity/me/permissions`, including denied resources with empty scope.
- Access-token lifetime default lowered from 1 hour to **15 minutes**, documented as the revocation bound. Refresh already rotates both tokens and re-reads roles, so no client change is required; refresh-token lifetime is unchanged at 2 hours.
- Optional per-principal security stamp with a per-node cache and configurable interval, dependent on no new infrastructure.
- Typed security notifications for role and assignment mutations, per [ADR 0007](../../docs/adr/0007-publish-audit-ready-security-notifications.md).

### Milestone 5: Tenancy Hardening

Independently justified as a latent-defect fix; sequenced last so the unresolved isolation-boundary question does not block delivery.

- Per-tenant composite unique indexes for role, user, and application names, with migrations across all five providers.
- Tenant filtering in `MemoryStore`, so the default stores isolate rather than relying on the Entity Framework path.
- Explicit tenant filters on role and user listing, and on user creation.

Excluded: `Elsa.Secrets` tenancy, tracked as [#7972](https://github.com/elsa-workflows/elsa-core/issues/7972).

## Post-Design Constitution Re-check

To be completed after Phase 1 artefacts exist, with particular attention to Principle VII: the resource tree must not acquire depth or verbs beyond what endpoints actually distinguish.

## Complexity Tracking

| Item | Justification | Exit condition |
| --- | --- | --- |
| Obsolete `ConfigurePermissions(params string[])` retained indefinitely | Third-party modules outside this repository must keep compiling across the upgrade. | Removed at the next major version. |
| Implicit unverified descriptors for unrecognized third-party permissions | Failing a host at boot because a module the operator does not own uses an unknown string is disproportionate; the existing `unknown_permission_descriptor` precedent warns instead. | None; permanent, with the gap visible in the catalog. |
| Wildcard grants confer forward reach on the resource axis | This is the property that makes section-wide grants viable and removes the need for a second gate. | None; mitigated by catalog reach reporting. |
