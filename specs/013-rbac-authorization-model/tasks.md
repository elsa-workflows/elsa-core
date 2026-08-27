# Tasks: Authorization Model

**Input**: Design documents from `/specs/013-rbac-authorization-model/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `contracts/permissions.md`, `contracts/rest-api.md`

**Tests**: Required. The fail-closed coverage gate (T041) is itself a deliverable and must land in the same milestone as the cutover.

**Organization**: Phases map to the milestones in `plan.md`. Every task is tagged with the user story from `spec.md` it serves. All paths are relative to the `elsa-core` repository.

**Sizing note**: Phase 3 is one pull request per module. Module endpoint-file counts, from a full census: Workflows.Api 78, ExternalAuthentication 13, Identity 11, Secrets 10, Labels 7, Diagnostics.OpenTelemetry 7, Tenants 6, Dashboard.Api 5, Alterations 5, Resilience 3, Diagnostics.StructuredLogs 3, Bpmn.Interchange 3, AI.Host 3, Shells.Api 2, Diagnostics.ConsoleLogs 2, Http.Webhooks 1, Expressions.JavaScript 1.

## Format: `[ID] [P?] [Story] Description`

> **Reconciled 2026-08-27.** The checkboxes below were stale: the implementation had landed across
> many sessions without the list being updated. Every task was re-verified against the code on this
> date — 52 were confirmed complete and ticked; the 13 that remain open carry an inline
> note naming the missing evidence. Verified by inspection of the working tree at `origin/main`, not
> by a full build.


- **[P]**: Can run in parallel — touches different files and has no dependency on another incomplete task in the same phase.
- **[Story]**: Maps to a user story in `spec.md` (US1–US9).

---

## Phase 1: Model and Evaluator

**Purpose**: The permission model and the single decision point. Entirely additive — nothing changes behavior, and no endpoint is touched.

- [x] T001 [P] [US1] Define `Permission` as a `(Resource, Verb)` pair with canonical parse and format in `src/common/Elsa.Api.Common/Authorization/Permission.cs`. Reject strings containing a comma, per the persistence constraint. **Verified done 2026-08-27.**
- [x] T002 [P] [US1] Declare the recommended core verbs — `view`, `create`, `update`, `write`, `delete`, `execute` — as constants in `src/common/Elsa.Api.Common/Authorization/CoreVerbs.cs`. **Verified done 2026-08-27.**
- [x] T003 [US1] Implement `PermissionMatcher` in `src/common/Elsa.Api.Common/Authorization/PermissionMatcher.cs`: exact match on both axes; a trailing `*` on the resource axis matching the named node **and all descendants at any depth**; `*` on the verb axis matching any verb. **Verified done 2026-08-27.**
- [x] T004 [US1] Define `IPermissionEvaluator` and its implementation in `src/common/Elsa.Api.Common/Authorization/`, resolving a principal's grants from the `permissions` claim and evaluating them through `PermissionMatcher`. **Verified done 2026-08-27.**
- [x] T005 [US2] Add `PermissionRequirement` and `PermissionAuthorizationHandler` in `src/common/Elsa.Api.Common/Authorization/`, replacing the FastEndpoints exact-match permission check as the enforcement path. **Verified done 2026-08-27.**
- [x] T006 [US3] Promote `PermissionDescriptor`, `IPermissionDescriptorProvider`, `IPermissionDescriptorRegistry` and `DefaultPermissionDescriptorRegistry` from `Elsa.ExternalAuthentication` into `src/common/Elsa.Api.Common/Permissions/`, leaving type-forwarding shims so External Authentication keeps compiling. **Verified done 2026-08-27.**
- [x] T007 [US3] Extend `PermissionDescriptor` with the verbs a resource supports, so the catalog can drive a role editor and validate submitted grants. **Verified done 2026-08-27.**
- [x] T008 [P] [US1] Unit-test the matcher table in `test/unit/Elsa.Api.Common.UnitTests/Authorization/PermissionMatcherTests.cs`: exact match on each axis; subtree wildcard covering the node itself and descendants; verb wildcard; `*:*`; absence denying; a wildcard covering a newly registered resource and a newly registered verb. **Verified done 2026-08-27.**
- [x] T009 [P] [US1] Unit-test `IPermissionEvaluator` for union-across-roles semantics and for the absence of verb implication (FR-009). **Verified done 2026-08-27.**

---

## Phase 2: Catalog Coverage

**Purpose**: Every module declares its resources and verbs. Still additive.

Each module task creates `Permissions/<Module>Permissions.cs` following the pattern already proven in `src/modules/Elsa.ExternalAuthentication/Permissions/ExternalAuthenticationPermissions.cs` — constants and descriptors colocated — refined to **one constant per resource**, with verbs supplied separately. Resources and verbs come from `contracts/permissions.md`.

- [x] T010 [P] [US3] `Elsa.Workflows.Api` — 20 resources spanning `workflows/definitions`, `.../versions`, `.../labels`, `workflows/instances`, `activity-executions`, `runtime`, `bookmark-queue/dead-letters`, `events`, `tasks`, `tests`, the nine `descriptors/*`, and `scripting/javascript`. **Verified done 2026-08-27.**
- [x] T011 [P] [US3] `Elsa.Identity` — `identity/users`, `identity/roles`, `identity/applications`. **Verified done 2026-08-27.**
- [x] T012 [P] [US3] `Elsa.Secrets` — `secrets`. Retire the unused `use:`, `import:` and `export:` constants. **Verified done 2026-08-27.**
- [x] T013 [P] [US3] `Elsa.ExternalAuthentication` — `connections`, `descriptors`, `identity-links`, `sessions`, `policies`, `policies/default-roles`, `provider-trust`, `permission-grants`. Correct the `roles:assign` descriptor text to describe what it actually guards. **Verified done 2026-08-27.**
- [x] T014 [P] [US3] `Elsa.Labels` — `labels`. **Verified done 2026-08-27.**
- [x] T015 [P] [US3] `Elsa.Tenants` — `tenants`. **Verified done 2026-08-27.**
- [x] T016 [P] [US3] `Elsa.Alterations` — `alterations`. **Verified done 2026-08-27.**
- [x] T017 [P] [US3] `Elsa.Dashboard.Api` — `dashboard`. **Verified done 2026-08-27.**
- [x] T018 [P] [US3] `Elsa.Resilience` — `resilience/retries`, `resilience/strategies`, `resilience/simulation`. **Verified done 2026-08-27.**
- [x] T019 [P] [US3] `Elsa.Diagnostics.ConsoleLogs`, `Elsa.Diagnostics.StructuredLogs`, `Elsa.Diagnostics.OpenTelemetry` — the three `diagnostics/*` resources. Retire the unused `ingest:` constant. **Verified done 2026-08-27.**
- [x] T020 [P] [US3] `Elsa.AI.Host` — `ai/chat`, `ai/tools`, `ai/capabilities`. Retire the unused `ai:proposals:*` and `ai:tools:manage` constants. **Verified done 2026-08-27.**
- [x] T021 [P] [US3] `Elsa.Shells.Api` and `Elsa.Workflows.Api` platform resources — `system/shells`, `system/features`. **Verified done 2026-08-27.**
- [x] T022 [US3] Reduce `src/common/Elsa.Api.Common/PermissionNames.cs` to the claim type and the whole-vocabulary grant; move the workflow-runtime and bookmark-queue constants to the Workflows.Api declarations. Remove the dead `AdminRoleName`/`ReaderRoleName`/`WriteRoleName` fields from `EndpointSecurityOptions.cs`. **Verified done 2026-08-27.**
- [x] T022a [US3] Wire descriptor validation into the role write paths (`Endpoints/Roles/Create`, `Endpoints/Roles/Update`), which today persist `request.Permissions` after only the caller-subset check. Reject a concrete resource with no registered descriptor and a concrete verb outside that resource's supported verbs; accept structurally valid wildcard segments, including ones that currently match nothing. Covers FR-012a. **Verified done 2026-08-27.**
- [ ] T022b [P] [US3] Unit-test role-authoring validation: unknown concrete resource rejected, unsupported concrete verb rejected, `workflows/*:view` accepted, `*:*` accepted, and a wildcard matching no installed module accepted. **Still open (verified 2026-08-27)** — no role-authoring validation tests found in `test/unit/Elsa.Identity.UnitTests`.
- [x] T023 [US3] Implement `GET /identity/permissions` in `src/modules/Elsa.Identity/Endpoints/Permissions/List/Endpoint.cs`, returning core verbs, every registered resource with supported verbs, category and display metadata, and a `nonCoreVerbs` marker. Contract: `contracts/rest-api.md`. Requires `identity/roles:view`. **Verified done 2026-08-27.**
- [x] T024 [US1] Implement `GET /identity/permissions/reach` in `src/modules/Elsa.Identity/Endpoints/Permissions/Reach/Endpoint.cs`, reporting the resources a wildcard grant currently covers. This is the mitigation for forward reach on the resource axis. **Verified done 2026-08-27.**
- [ ] T025 [P] [US3] Integration-test the catalog endpoint for descriptor consistency only: every registered resource exposes display metadata, a category and a non-empty supported-verb list, verbs outside the core set are marked, and no two modules register the same resource. **Endpoint-to-descriptor resolution is deliberately not asserted here** — in Phase 2 endpoints still declare legacy strings, so that assertion belongs to the cutover gate T041. **Still open (verified 2026-08-27)** — no catalog descriptor-consistency integration test found.

---

## Phase 3: Cutover

**Purpose**: The breaking change. One pull request per module, landing in any order.

No migration scaffold is required — the obsolete declaration path (T027) translates legacy *endpoint declarations*, while a legacy *stored grant* still fails to match, and the seeded admin `*` satisfies every endpoint throughout. See `research.md` D20.

- [x] T026a [US2] Add an explicit authenticated-only declaration to the base classes in `src/common/Elsa.Api.Common/Abstractions/Endpoints.cs`, so FR-019's third state is expressible and the coverage gate can distinguish a deliberate choice from an omission. **Verified done 2026-08-27.**
- [x] T026 [US8] Add `RequirePermission(string resource, string verb)` to the six base classes in `src/common/Elsa.Api.Common/Abstractions/Endpoints.cs`, collapsing the six copy-pasted `ConfigurePermissions` bodies into one shared implementation. **Verified done 2026-08-27.**
- [ ] T027 [US8] Keep `ConfigurePermissions(params string[])` as `[Obsolete]` but functional: resolve legacy strings through the migration table; register an implicit descriptor marked unverified and log a warning for anything unresolvable, rather than failing the host at boot. Follows the existing `unknown_permission_descriptor` precedent in `DefaultPermissionGrantResolver`. **Still open (verified 2026-08-27)** — `ConfigurePermissions` is preserved and functional, but is **not** marked `[Obsolete]` and does not resolve legacy strings through a migration table, register an unverified descriptor, or log a warning — it simply prepends `PermissionNames.All`.
- [x] T028 [US1] Migrate `Elsa.Workflows.Api` — `WorkflowDefinitions` (31 files) to `RequirePermission`. **Verified done 2026-08-27.**
- [x] T028a [P] [US1] Migrate `Elsa.Workflows.Api` — `WorkflowInstances`, `ActivityExecutions`, `ActivityExecutionSummaries` (20 files). **Verified done 2026-08-27.**
- [x] T028b [P] [US1] Migrate `Elsa.Workflows.Api` — `RuntimeAdmin`, `BookmarkQueueDeadLetters`, `Bookmarks`, `Events`, `Tasks`, `Tests`, `Features` (15 files). **Verified done 2026-08-27.**
- [x] T028c [P] [US1] Migrate `Elsa.Workflows.Api` — the nine descriptor folders and `Scripting` (12 files). **Verified done 2026-08-27.**
- [x] T029 [P] [US1] Migrate `Elsa.ExternalAuthentication` endpoints (13 files, 34 endpoint classes — several classes per file). **Verified done 2026-08-27.**
- [x] T030 [P] [US1] Migrate `Elsa.Identity` endpoints (11 files). **Verified done 2026-08-27.**
- [x] T031 [P] [US1] Migrate `Elsa.Secrets` endpoints (10 files). **Verified done 2026-08-27.**
- [x] T032 [P] [US1] Migrate `Elsa.Labels` (7) and `Elsa.Tenants` (6) endpoints. **Verified done 2026-08-27.**
- [x] T033 [P] [US1] Migrate `Elsa.Diagnostics.OpenTelemetry` (7), `Elsa.Diagnostics.StructuredLogs` (3) and `Elsa.Diagnostics.ConsoleLogs` (2) endpoints. **Verified done 2026-08-27.**
- [x] T034 [P] [US1] Migrate `Elsa.Dashboard.Api` (5) and `Elsa.Alterations` (5) endpoints. **Verified done 2026-08-27.**
- [x] T035 [P] [US1] Migrate `Elsa.Resilience` (3), `Elsa.Bpmn.Interchange` (3) and `Elsa.AI.Host` (3) endpoints. **Verified done 2026-08-27.**
- [x] T036 [P] [US1] Migrate `Elsa.Shells.Api` (2), `Elsa.Http.Webhooks` (1) and `Elsa.Expressions.JavaScript` (1) endpoints. **Verified done 2026-08-27.**
- [x] T037 [US2] Declare the two endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/Logout.cs` separately: `Logout` as authenticated-only (it reads the external session claim from the principal), `ContinueLogout` as `AllowAnonymous` (the route handle carries the authority, matching every other broker callback). `ContinueLogout` inheriting the authenticated default today is a probable live bug — the identity provider redirects the browser there during upstream logout, potentially after the Elsa session is gone. Fix tracked as #7976. **Verified done 2026-08-27.**
- [ ] T038 [US1] Replace the hand-rolled `PermissionNames.ClaimType` claim inspections with `IPermissionEvaluator` calls across `Elsa.ExternalAuthentication` (5 files), `Elsa.Workflows.Api` (2), `Elsa.Identity` (2), `Elsa.Api.Common` (1) and `Elsa.AI.Host` (1). Note `AIHttpContextIdentity` currently compares case-insensitively while every other site is ordinal; the evaluator standardises this. **Still open (verified 2026-08-27)** — two hand-rolled claim inspections remain: `Elsa.AI.Host/Endpoints/AI/AIHttpContextIdentity.cs` (still case-insensitive) and `Elsa.Identity/Services/RoleDeletionCoordinator.cs`.
- [x] T038a [P] [US1] Unit-test `RoleAuthorizationService` under wildcard containment, extending the existing tests: a held `workflows/*:view` may grant `workflows/definitions:view`; a held `workflows/definitions:view` may **not** grant `workflows/*:view`; a held concrete grant may not grant an unsupported verb; `*:*` may grant anything. **Verified done 2026-08-27.**
- [x] T039 [US1] Route the four SignalR hub permission checks through `IPermissionEvaluator` — `WorkflowInstanceHub`, `ElsaConsoleLogStreamHubAuthorizer`, `StructuredLogsHub`, `OpenTelemetryHub` — replacing hard-coded arrays such as `["*", "read:*", "read:workflow-instances"]`. **Verified done 2026-08-27.**
- [ ] T040 [US1] Replace the three `Policies(IdentityPolicyNames.SecurityRoot)` usages in `Elsa.Identity` (`Secrets/Hash`, `Roles/Create`, `Applications/Create`) and remove the obsolete policy. **Scope note**: per FR-017, the 15 mid-handler `AuthorizeAsync(..., NotReadOnlyPolicy)` calls in Workflows.Api are *not* in scope — read-only mode is a separate axis from permissions and keeps its own check. **Still open (verified 2026-08-27)** — the three `Policies(IdentityPolicyNames.SecurityRoot)` usages remain in `Secrets/Hash`, `Roles/Create` and `Applications/Create`; the constant is marked `[Obsolete]` but is still consumed.
- [x] T041 [US2] Add the fail-closed coverage gate in `test/unit/Elsa.Api.Common.UnitTests/Authorization/EndpointCoverageTests.cs`: enumerate every in-repository `ElsaEndpoint*` type by reflection and assert each declares exactly one of a permission resolving to a registered descriptor, `AllowAnonymous`, or authenticated-only. No exemption list. **Verified done 2026-08-27.** Note: landed as the shared `EndpointCoverage.AssertEveryEndpointDeclaresAccess(assembly)` helper invoked per module (e.g. `test/unit/Elsa.Identity.UnitTests/Authorization/EndpointCoverageTests.cs`) rather than a single test in `Elsa.Api.Common.UnitTests`.
- [ ] T042 [US5] Update `DefaultAccessTokenIssuer` and `DefaultElsaTokenService` to emit new-format `{resource}:{verb}` claims, and update `DefaultApiKeyProvider`, `AdminApiKeyProvider`, `LocalHostPermissionRequirement` and `DefaultExternalAuthenticationTokenIssuer` to match. **Partially done (verified 2026-08-27)** — the issuers emit role-derived permissions, but `LocalHostPermissionRequirement` still carries the legacy `BootstrapPermissions` list (`create:application`, `create:user`, `create:role`). Those satisfy none of the three endpoints they exist to unlock, all of which now declare structured permissions, so the localhost bootstrap path grants nothing. Tracked for repair.
- [x] T043 [US5] Add a startup validator that scans stored roles and logs every permission that does not resolve, identified by role name. Fails closed and loudly; the seeded `*` grant is unaffected so an instance cannot be locked out. **Verified done 2026-08-27.**
- [ ] T044 [P] [US5] Regression-test that legacy stored grants no longer authorize, that `*` still authorizes everything, and that a legacy *endpoint declaration* still resolves through T027. **Still open (verified 2026-08-27)** — no legacy-stored-grant regression test found.

---

## Phase 4: Introspection, Revocation, and Audit

- [x] T045 [US4] Implement `GET /identity/me/permissions` in `src/modules/Elsa.Identity/Endpoints/Me/Permissions/Endpoint.cs`. Every registered resource is present, including those the caller cannot access, carrying an empty verb list. Wildcard grants are resolved to concrete verbs per covered resource. Contract: `contracts/rest-api.md`. **Verified done 2026-08-27.**
- [x] T046 [US6] Lower the default `AccessTokenLifetime` in `src/modules/Elsa.Identity/Options/IdentityTokenOptions.cs` from 1 hour to 15 minutes. Refresh already rotates both tokens and re-reads roles, so no client change is required; leave `RefreshTokenLifetime` at 2 hours. **Verified done 2026-08-27.**
- [x] T047 [US6] Add an optional per-principal security stamp: a monotonic value bumped on any role, grant or membership change, carried as a claim and compared against a per-node cached value under a configurable interval. Must not depend on cross-node cache invalidation, which Elsa does not have. **Verified done 2026-08-27.**
- [x] T047a [US6] Ship the stamp's persistence with it: if the stamp lives on `User`, this task includes the Identity migrations for all five EF providers, so Phase 4 remains independently shippable and does not depend on T053 in Phase 5. Prefer a store that avoids an entity-schema change if one fits. **Verified done 2026-08-27.** Note: satisfied without an entity-schema change — the stamp is computed by `PermissionStampCalculator` rather than persisted on `User`, so no Identity migrations were required.
- [x] T048 [US7] Publish typed security notifications for role create, update and delete, and for user role assignment and removal, through `INotificationSender` per ADR 0007. This feature owns no audit persistence. **Verified done 2026-08-27.**
- [ ] T049 [US1] Promote the deployment grant boundary (`PermissionGrantOptions.AllowedPermissions` / `DeniedPermissions`, currently in `ExternalAuthenticationOptions`) into `Elsa.Identity` so it applies to all grant paths, and make it wildcard-aware. **Still open (verified 2026-08-27)** — `PermissionGrantOptions` still lives in `Elsa.ExternalAuthentication`; not promoted to `Elsa.Identity`.
- [ ] T050 [P] [US4] Integration-test `/me/permissions` for a role holding partial verbs, for a wildcard grant resolving to concrete verbs, and for denied resources appearing with an empty list. **Still open (verified 2026-08-27)** — no `/me/permissions` integration test found.
- [ ] T051 [P] [US6] Integration-test that a revoked role stops authorizing on the next token issuance, and immediately within the stamp interval when the stamp is enabled. **Still open (verified 2026-08-27)** — no revocation / stamp-interval integration test found.

---

## Phase 5: Tenancy Hardening

**Purpose**: Close the gaps that make "roles are per tenant" unsafe today. Independently justified as latent-defect fixes; sequenced last so the unresolved isolation-boundary question does not block delivery.

- [x] T052 [US9] Replace the global unique indexes on `User.Name`, `Role.Name`, `Application.ClientId` and `Application.Name` with per-tenant composite indexes in `src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs`. **Verified done 2026-08-27.**
- [x] T053 [US9] Generate Identity migrations for all five providers — Sqlite, SqlServer, PostgreSql, MySql, Oracle. **Verified done 2026-08-27.**
- [ ] T054 [US9] Make `src/modules/Elsa.Common/Services/MemoryStore.cs` tenant-aware so the default `IUserStore` and `IRoleStore` isolate, rather than isolation existing only on the Entity Framework path when `TenantsOptions.IsEnabled`. **Still open (verified 2026-08-27)** — `Elsa.Common/Services/MemoryStore.cs` has no tenant awareness (no `TenantId`/`ITenantAccessor` reference).
- [x] T055 [US9] Apply explicit tenant filters in `Endpoints/Users/List` and `Endpoints/Roles/List`, which currently pass an empty filter, and set `TenantId` explicitly in `UserManager.CreateUserAsync` rather than relying on the saving handler. **Verified done 2026-08-27.**
- [ ] T056 [P] [US9] Integration-test that two tenants can each hold a role named `Admin`, and that roles and users listed in one tenant are invisible in another, against both the Entity Framework and in-memory stores. **Still open (verified 2026-08-27)** — no cross-tenant identity isolation test verified for both EF and in-memory stores.

---

## Phase 6: Documentation and Release Readiness

- [x] T057 [US5] Write `doc/migrations/authorization-model.md` from the mapping table in `contracts/permissions.md`, following the shape of `doc/migrations/external-authentication-persistence.md`. It must state prominently that **the migration expands rather than renames** — several legacy permissions map to more than one new permission, and a one-for-one substitution silently narrows roles. **Done 2026-08-24** — `doc/migrations/authorization-model.md` leads with the three things that are not a simple rename, and tells operators to confirm `*` still works first.
- [x] T058 [US5] Document in the same file that `read:*` and `exec:*` become materially **more** powerful as `*:view` and `*:execute`, and that any role holding them needs human review rather than an automated rewrite. **Done 2026-08-24**
- [x] T059 [US5] Document in the same file the removal of `exec:csharp-expressions` and `exec:python-expressions` as a **deliberate reduction in control**: where host code is enabled, any author who may write definitions may use C# and Python. Link #7975. **Done 2026-08-24** — linked to #7975.
- [x] T060 Record the model in `doc/adr/0025-two-axis-authorization-model.md`: both axes open, wildcards as the only forward reach, no aggregates and no verb implication, and the rejection of a closed verb enumeration. **Done 2026-08-24** — `doc/adr/0025-two-axis-authorization-model.md`.
- [x] T061 [P] Update `doc/wiki/identity-tenancy-security.md`, replacing the Secrets-only route table with a pointer to the catalog endpoint as the authoritative source. **Done 2026-08-24**
- [x] T062 Resolve the five module-owner questions and fold the answers into the vocabulary. **Done 2026-08-23** — outcomes recorded at the end of `contracts/permissions.md` and as D25/D26 in `research.md`. Produced #7976 and #7977, and added FR-019's third declaration state (T026a).
- [ ] T063 Run `dotnet build Elsa.sln` and the full test suite, confirm T041 passes with zero exemptions beyond documented anonymous endpoints, and verify the quickstart scenario end to end. **Still open (verified 2026-08-27)** — full `dotnet build Elsa.sln` + full suite run not recorded.

---

## Dependencies

- **Phase 1 blocks everything.** No descriptor work starts before the matcher and evaluator pass their tests.
- **Phase 2 blocks Phase 3**: an endpoint cannot declare a resource that has no descriptor, because T041 asserts resolution.
- **T062 preceded Phase 2** and is complete, so the vocabulary is settled before any module declares constants against it.
- **T026 and T027 block T028–T036.** Within that range the module tasks are independent and land in any order.
- **T041 lands with the last module migration**, not before, or trunk fails while migration is in flight.
- **Phase 5 is independent of Phases 3 and 4** and may run in parallel with either.
- **T057–T059 must land in the same release as Phase 3**, since they document its breaking changes.

## Parallel guidance

Phase 2 is almost entirely parallel — sixteen module tasks touching disjoint files. Phase 3 is parallel after T026 and T027, with T028 (Workflows.Api, 78 files) the critical path and a candidate for splitting by endpoint folder. Phase 5 touches persistence and common services and should not run concurrently with itself.
