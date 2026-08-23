# Tasks: Authorization Model

**Input**: Design documents from `/specs/013-rbac-authorization-model/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `contracts/permissions.md`, `contracts/rest-api.md`

**Tests**: Required. The fail-closed coverage gate (T041) is itself a deliverable and must land in the same milestone as the cutover.

**Organization**: Phases map to the milestones in `plan.md`. Every task is tagged with the user story from `spec.md` it serves. All paths are relative to the `elsa-core` repository.

**Sizing note**: Phase 3 is one pull request per module. Module endpoint-file counts, from a full census: Workflows.Api 78, ExternalAuthentication 13, Identity 11, Secrets 10, Labels 7, Diagnostics.OpenTelemetry 7, Tenants 6, Dashboard.Api 5, Alterations 5, Resilience 3, Diagnostics.StructuredLogs 3, Bpmn.Interchange 3, AI.Host 3, Shells.Api 2, Diagnostics.ConsoleLogs 2, Http.Webhooks 1, Expressions.JavaScript 1.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel — touches different files and has no dependency on another incomplete task in the same phase.
- **[Story]**: Maps to a user story in `spec.md` (US1–US9).

---

## Phase 1: Model and Evaluator

**Purpose**: The permission model and the single decision point. Entirely additive — nothing changes behaviour, and no endpoint is touched.

- [ ] T001 [P] [US1] Define `Permission` as a `(Resource, Verb)` pair with canonical parse and format in `src/common/Elsa.Api.Common/Authorization/Permission.cs`. Reject strings containing a comma, per the persistence constraint.
- [ ] T002 [P] [US1] Declare the recommended core verbs — `view`, `create`, `update`, `write`, `delete`, `execute` — as constants in `src/common/Elsa.Api.Common/Authorization/CoreVerbs.cs`.
- [ ] T003 [US1] Implement `PermissionMatcher` in `src/common/Elsa.Api.Common/Authorization/PermissionMatcher.cs`: exact match on both axes; a trailing `*` on the resource axis matching the named node **and all descendants at any depth**; `*` on the verb axis matching any verb.
- [ ] T004 [US1] Define `IPermissionEvaluator` and its implementation in `src/common/Elsa.Api.Common/Authorization/`, resolving a principal's grants from the `permissions` claim and evaluating them through `PermissionMatcher`.
- [ ] T005 [US2] Add `PermissionRequirement` and `PermissionAuthorizationHandler` in `src/common/Elsa.Api.Common/Authorization/`, replacing the FastEndpoints exact-match permission check as the enforcement path.
- [ ] T006 [US3] Promote `PermissionDescriptor`, `IPermissionDescriptorProvider`, `IPermissionDescriptorRegistry` and `DefaultPermissionDescriptorRegistry` from `Elsa.ExternalAuthentication` into `src/common/Elsa.Api.Common/Permissions/`, leaving type-forwarding shims so External Authentication keeps compiling.
- [ ] T007 [US3] Extend `PermissionDescriptor` with the verbs a resource supports, so the catalog can drive a role editor and validate submitted grants.
- [ ] T008 [P] [US1] Unit-test the matcher table in `test/unit/Elsa.Api.Common.UnitTests/Authorization/PermissionMatcherTests.cs`: exact match on each axis; subtree wildcard covering the node itself and descendants; verb wildcard; `*:*`; absence denying; a wildcard covering a newly registered resource and a newly registered verb.
- [ ] T009 [P] [US1] Unit-test `IPermissionEvaluator` for union-across-roles semantics and for the absence of verb implication (FR-009).

---

## Phase 2: Catalog Coverage

**Purpose**: Every module declares its resources and verbs. Still additive.

Each module task creates `Permissions/<Module>Permissions.cs` following the pattern already proven in `src/modules/Elsa.ExternalAuthentication/Permissions/ExternalAuthenticationPermissions.cs` — constants and descriptors colocated — refined to **one constant per resource**, with verbs supplied separately. Resources and verbs come from `contracts/permissions.md`.

- [ ] T010 [P] [US3] `Elsa.Workflows.Api` — 20 resources spanning `workflows/definitions`, `.../versions`, `.../labels`, `workflows/instances`, `activity-executions`, `runtime`, `bookmark-queue/dead-letters`, `events`, `tasks`, `tests`, the nine `descriptors/*`, and `scripting/javascript`.
- [ ] T011 [P] [US3] `Elsa.Identity` — `identity/users`, `identity/roles`, `identity/applications`.
- [ ] T012 [P] [US3] `Elsa.Secrets` — `secrets`. Retire the unused `use:`, `import:` and `export:` constants.
- [ ] T013 [P] [US3] `Elsa.ExternalAuthentication` — `connections`, `identity-links`, `sessions`, `policies`, `policies/default-roles`, `provider-trust`, `permission-grants`.
- [ ] T014 [P] [US3] `Elsa.Labels` — `labels`.
- [ ] T015 [P] [US3] `Elsa.Tenants` — `tenants`.
- [ ] T016 [P] [US3] `Elsa.Alterations` — `alterations`.
- [ ] T017 [P] [US3] `Elsa.Dashboard.Api` — `dashboard`.
- [ ] T018 [P] [US3] `Elsa.Resilience` — `resilience/retries`, `resilience/strategies`, `resilience/simulation`.
- [ ] T019 [P] [US3] `Elsa.Diagnostics.ConsoleLogs`, `Elsa.Diagnostics.StructuredLogs`, `Elsa.Diagnostics.OpenTelemetry` — the three `diagnostics/*` resources. Retire the unused `ingest:` constant.
- [ ] T020 [P] [US3] `Elsa.AI.Host` — `ai/chat`, `ai/tools`, `ai/capabilities`. Retire the unused `ai:proposals:*` and `ai:tools:manage` constants.
- [ ] T021 [P] [US3] `Elsa.Shells.Api` and `Elsa.Workflows.Api` platform resources — `system/shells`, `system/features`.
- [ ] T022 [US3] Reduce `src/common/Elsa.Api.Common/PermissionNames.cs` to the claim type and the whole-vocabulary grant; move the workflow-runtime and bookmark-queue constants to the Workflows.Api declarations. Remove the dead `AdminRoleName`/`ReaderRoleName`/`WriteRoleName` fields from `EndpointSecurityOptions.cs`.
- [ ] T023 [US3] Implement `GET /identity/permissions` in `src/modules/Elsa.Identity/Endpoints/Permissions/List/Endpoint.cs`, returning core verbs, every registered resource with supported verbs, category and display metadata, and a `nonCoreVerbs` marker. Contract: `contracts/rest-api.md`. Requires `identity/roles:view`.
- [ ] T024 [US1] Implement `GET /identity/permissions/reach` in `src/modules/Elsa.Identity/Endpoints/Permissions/Reach/Endpoint.cs`, reporting the resources a wildcard grant currently covers. This is the mitigation for forward reach on the resource axis.
- [ ] T025 [P] [US3] Integration-test the catalog endpoint: every resource declared by an in-repository endpoint resolves to a registered descriptor, and non-core verbs are marked.

---

## Phase 3: Cutover

**Purpose**: The breaking change. One pull request per module, landing in any order.

No migration scaffold is required — the obsolete declaration path (T027) translates legacy *endpoint declarations*, while a legacy *stored grant* still fails to match, and the seeded admin `*` satisfies every endpoint throughout. See `research.md` D20.

- [ ] T026 [US8] Add `RequirePermission(string resource, string verb)` to the six base classes in `src/common/Elsa.Api.Common/Abstractions/Endpoints.cs`, collapsing the six copy-pasted `ConfigurePermissions` bodies into one shared implementation.
- [ ] T027 [US8] Keep `ConfigurePermissions(params string[])` as `[Obsolete]` but functional: resolve legacy strings through the migration table; register an implicit descriptor marked unverified and log a warning for anything unresolvable, rather than failing the host at boot. Follows the existing `unknown_permission_descriptor` precedent in `DefaultPermissionGrantResolver`.
- [ ] T028 [US1] Migrate `Elsa.Workflows.Api` endpoints (78 files) to `RequirePermission`. Largest single unit; may be split by endpoint folder.
- [ ] T029 [P] [US1] Migrate `Elsa.ExternalAuthentication` endpoints (13 files, 34 endpoint classes — several classes per file).
- [ ] T030 [P] [US1] Migrate `Elsa.Identity` endpoints (11 files).
- [ ] T031 [P] [US1] Migrate `Elsa.Secrets` endpoints (10 files).
- [ ] T032 [P] [US1] Migrate `Elsa.Labels` (7) and `Elsa.Tenants` (6) endpoints.
- [ ] T033 [P] [US1] Migrate `Elsa.Diagnostics.OpenTelemetry` (7), `Elsa.Diagnostics.StructuredLogs` (3) and `Elsa.Diagnostics.ConsoleLogs` (2) endpoints.
- [ ] T034 [P] [US1] Migrate `Elsa.Dashboard.Api` (5) and `Elsa.Alterations` (5) endpoints.
- [ ] T035 [P] [US1] Migrate `Elsa.Resilience` (3), `Elsa.Bpmn.Interchange` (3) and `Elsa.AI.Host` (3) endpoints.
- [ ] T036 [P] [US1] Migrate `Elsa.Shells.Api` (2), `Elsa.Http.Webhooks` (1) and `Elsa.Expressions.JavaScript` (1) endpoints.
- [ ] T037 [US2] Give `Logout` and `ContinueLogout` in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/Logout.cs` an explicit declaration. Both currently declare neither a permission nor `AllowAnonymous` and inherit FastEndpoints' authenticated-without-permission default, so both fail T041. Confirm the intended declaration with the module owner first (contract question 5).
- [ ] T038 [US1] Replace the hand-rolled `PermissionNames.ClaimType` claim inspections with `IPermissionEvaluator` calls across `Elsa.ExternalAuthentication` (5 files), `Elsa.Workflows.Api` (2), `Elsa.Identity` (2), `Elsa.Api.Common` (1) and `Elsa.AI.Host` (1). Note `AIHttpContextIdentity` currently compares case-insensitively while every other site is ordinal; the evaluator standardises this.
- [ ] T039 [US1] Route the four SignalR hub permission checks through `IPermissionEvaluator` — `WorkflowInstanceHub`, `ElsaConsoleLogStreamHubAuthorizer`, `StructuredLogsHub`, `OpenTelemetryHub` — replacing hard-coded arrays such as `["*", "read:*", "read:workflow-instances"]`.
- [ ] T040 [US1] Replace the three `Policies(IdentityPolicyNames.SecurityRoot)` usages in `Elsa.Identity` (`Secrets/Hash`, `Roles/Create`, `Applications/Create`) and remove the obsolete policy. **Scope note**: per FR-017, the 15 mid-handler `AuthorizeAsync(..., NotReadOnlyPolicy)` calls in Workflows.Api are *not* in scope — read-only mode is a separate axis from permissions and keeps its own check.
- [ ] T041 [US2] Add the fail-closed coverage gate in `test/unit/Elsa.Api.Common.UnitTests/Authorization/EndpointCoverageTests.cs`: enumerate every in-repository `ElsaEndpoint*` type by reflection and assert each declares either a permission resolving to a registered descriptor, or `AllowAnonymous`.
- [ ] T042 [US5] Update `DefaultAccessTokenIssuer` and `DefaultElsaTokenService` to emit new-format `{resource}:{verb}` claims, and update `DefaultApiKeyProvider`, `AdminApiKeyProvider`, `LocalHostPermissionRequirement` and `DefaultExternalAuthenticationTokenIssuer` to match.
- [ ] T043 [US5] Add a startup validator that scans stored roles and logs every permission that does not resolve, identified by role name. Fails closed and loudly; the seeded `*` grant is unaffected so an instance cannot be locked out.
- [ ] T044 [P] [US5] Regression-test that legacy stored grants no longer authorize, that `*` still authorizes everything, and that a legacy *endpoint declaration* still resolves through T027.

---

## Phase 4: Introspection, Revocation, and Audit

- [ ] T045 [US4] Implement `GET /identity/me/permissions` in `src/modules/Elsa.Identity/Endpoints/Me/Permissions/Endpoint.cs`. Every registered resource is present, including those the caller cannot access, carrying an empty verb list. Wildcard grants are resolved to concrete verbs per covered resource. Contract: `contracts/rest-api.md`.
- [ ] T046 [US6] Lower the default `AccessTokenLifetime` in `src/modules/Elsa.Identity/Options/IdentityTokenOptions.cs` from 1 hour to 15 minutes. Refresh already rotates both tokens and re-reads roles, so no client change is required; leave `RefreshTokenLifetime` at 2 hours.
- [ ] T047 [US6] Add an optional per-principal security stamp: a monotonic value on the user, bumped on any role, grant or membership change, carried as a claim and compared against a per-node cached value under a configurable interval. Must not depend on cross-node cache invalidation, which Elsa does not have.
- [ ] T048 [US7] Publish typed security notifications for role create, update and delete, and for user role assignment and removal, through `INotificationSender` per ADR 0007. This feature owns no audit persistence.
- [ ] T049 [US1] Promote the deployment grant boundary (`PermissionGrantOptions.AllowedPermissions` / `DeniedPermissions`, currently in `ExternalAuthenticationOptions`) into `Elsa.Identity` so it applies to all grant paths, and make it wildcard-aware.
- [ ] T050 [P] [US4] Integration-test `/me/permissions` for a role holding partial verbs, for a wildcard grant resolving to concrete verbs, and for denied resources appearing with an empty list.
- [ ] T051 [P] [US6] Integration-test that a revoked role stops authorizing on the next token issuance, and immediately within the stamp interval when the stamp is enabled.

---

## Phase 5: Tenancy Hardening

**Purpose**: Close the gaps that make "roles are per tenant" unsafe today. Independently justified as latent-defect fixes; sequenced last so the unresolved isolation-boundary question does not block delivery.

- [ ] T052 [US9] Replace the global unique indexes on `User.Name`, `Role.Name`, `Application.ClientId` and `Application.Name` with per-tenant composite indexes in `src/modules/Elsa.Persistence.EFCore/Modules/Identity/Configurations.cs`.
- [ ] T053 [US9] Generate Identity migrations for all five providers — Sqlite, SqlServer, PostgreSql, MySql, Oracle.
- [ ] T054 [US9] Make `src/modules/Elsa.Common/Services/MemoryStore.cs` tenant-aware so the default `IUserStore` and `IRoleStore` isolate, rather than isolation existing only on the Entity Framework path when `TenantsOptions.IsEnabled`.
- [ ] T055 [US9] Apply explicit tenant filters in `Endpoints/Users/List` and `Endpoints/Roles/List`, which currently pass an empty filter, and set `TenantId` explicitly in `UserManager.CreateUserAsync` rather than relying on the saving handler.
- [ ] T056 [P] [US9] Integration-test that two tenants can each hold a role named `Admin`, and that roles and users listed in one tenant are invisible in another, against both the Entity Framework and in-memory stores.

---

## Phase 6: Documentation and Release Readiness

- [ ] T057 [US5] Write `docs/migrations/authorization-model.md` from the mapping table in `contracts/permissions.md`, following the shape of `docs/migrations/external-authentication-persistence.md`. It must state prominently that **the migration expands rather than renames** — several legacy permissions map to more than one new permission, and a one-for-one substitution silently narrows roles.
- [ ] T058 [US5] Document in the same file that `read:*` and `exec:*` become materially **more** powerful as `*:view` and `*:execute`, and that any role holding them needs human review rather than an automated rewrite.
- [ ] T059 [US5] Document in the same file the removal of `exec:csharp-expressions` and `exec:python-expressions` as a **deliberate reduction in control**: where host code is enabled, any author who may write definitions may use C# and Python. Link #7975.
- [ ] T060 Record the model in `docs/adr/00NN-two-axis-authorization-model.md`: both axes open, wildcards as the only forward reach, no aggregates and no verb implication, and the rejection of a closed verb enumeration.
- [ ] T061 [P] Update `doc/wiki/identity-tenancy-security.md`, replacing the Secrets-only route table with a pointer to the catalog endpoint as the authoritative source.
- [ ] T062 Resolve the five module-owner questions at the end of `contracts/permissions.md` and fold the answers into the vocabulary before Phase 2 begins.
- [ ] T063 Run `dotnet build Elsa.sln` and the full test suite, confirm T041 passes with zero exemptions beyond documented anonymous endpoints, and verify the quickstart scenario end to end.

---

## Dependencies

- **Phase 1 blocks everything.** No descriptor work starts before the matcher and evaluator pass their tests.
- **Phase 2 blocks Phase 3**: an endpoint cannot declare a resource that has no descriptor, because T041 asserts resolution.
- **T062 should precede Phase 2** — the module-owner answers change the vocabulary, and the vocabulary is expensive to change once modules have declared against it.
- **T026 and T027 block T028–T036.** Within that range the module tasks are independent and land in any order.
- **T041 lands with the last module migration**, not before, or trunk fails while migration is in flight.
- **Phase 5 is independent of Phases 3 and 4** and may run in parallel with either.
- **T057–T059 must land in the same release as Phase 3**, since they document its breaking changes.

## Parallel guidance

Phase 2 is almost entirely parallel — sixteen module tasks touching disjoint files. Phase 3 is parallel after T026 and T027, with T028 (Workflows.Api, 78 files) the critical path and a candidate for splitting by endpoint folder. Phase 5 touches persistence and common services and should not run concurrently with itself.
