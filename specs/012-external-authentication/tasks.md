# Tasks: External Authentication

**Input**: Design documents from `/specs/012-external-authentication/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, and `quickstart.md`

**Tests**: Tests are required by the specification and are written before the corresponding implementation.

**Organization**: Tasks are grouped by user story. Core paths are relative to the `elsa-core` repository; paths beginning with `/Users/sipke/Projects/Elsa/elsa-studio/` target the sibling Studio repository.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on another incomplete task in the same phase.
- **[Story]**: Maps the task to one of the user stories in `spec.md`.

## Phase 1: Setup

**Purpose**: Establish the Core and Studio project boundaries, references, test hosts, and dependency declarations.

- [x] T001 Create `src/modules/Elsa.ExternalAuthentication/Elsa.ExternalAuthentication.csproj`, register it in `Elsa.sln`, and add the protocol-neutral module folders defined by `plan.md`.
- [x] T002 [P] Create `src/modules/Elsa.ExternalAuthentication.OpenIdConnect/Elsa.ExternalAuthentication.OpenIdConnect.csproj`, register it in `Elsa.sln`, and reference the broker project.
- [x] T003 [P] Create `src/modules/Elsa.ExternalAuthentication.Secrets/Elsa.ExternalAuthentication.Secrets.csproj`, register it in `Elsa.sln`, and reference the broker and Secrets abstractions.
- [x] T004 [P] Create `test/unit/Elsa.ExternalAuthentication.UnitTests/Elsa.ExternalAuthentication.UnitTests.csproj` and register it in `Elsa.sln`.
- [x] T005 [P] Create `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj` with shared Identity EF and web-host fixtures and register it in `Elsa.sln`.
- [x] T006 [P] Add the maintained IdentityModel protocol dependencies and central versions required by the OpenID Connect adapter in `Directory.Packages.props`.
- [x] T007 [P] Create `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Elsa.Studio.ExternalAuthentication.csproj` and register it in `/Users/sipke/Projects/Elsa/elsa-studio/Elsa.Studio.sln`.
- [x] T008 [P] Create `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorServer/Elsa.Studio.ExternalAuthentication.BlazorServer.csproj` and `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/Elsa.Studio.ExternalAuthentication.BlazorWasm.csproj` and register both in `/Users/sipke/Projects/Elsa/elsa-studio/Elsa.Studio.sln`.
- [x] T009 [P] Create `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Elsa.Studio.ExternalAuthentication.Tests.csproj` and `/Users/sipke/Projects/Elsa/elsa-studio/tests/browser/ExternalAuthentication/README.md`, including the component and Playwright test dependencies.

---

## Phase 2: Foundational

**Purpose**: Implement the shared contracts and security boundaries that block every independently deliverable user story.

**Critical**: No user-story implementation starts until this phase passes its unit tests.

- [x] T010 [P] Define connection envelopes, source/scope/lifecycle enums, material revision inputs, claim projections, external identities, authentication clients, broker transactions, completion grants, sessions, and observations in `src/modules/Elsa.ExternalAuthentication/Models/` covering FR-001–FR-016, FR-020–FR-022, and FR-035–FR-043.
- [x] T011 [P] Define adapter, descriptor, registry, connection source, connection store, policy, grant source, secret resolver, atomic state, observation, link provisioning, and token service interfaces in `src/modules/Elsa.ExternalAuthentication/Contracts/` covering FR-017–FR-031 and FR-049–FR-065.
- [x] T012 [P] Define deployment options and secure defaults for clients, sources, policies, storage, lifetimes, claims, rate limits, egress, redirects, WebAssembly persistence, logout, and lockout guard in `src/modules/Elsa.ExternalAuthentication/Options/` covering FR-036, FR-042, FR-048, FR-058, FR-070–FR-078, and FR-092–FR-103.
- [x] T013 [P] Define fine-grained permission constants and permission descriptors in `src/modules/Elsa.ExternalAuthentication/Permissions/ExternalAuthenticationPermissions.cs` covering FR-030, FR-063–FR-064, FR-082, and FR-085.
- [x] T014 [P] Define immutable redacted security notification records in `src/modules/Elsa.ExternalAuthentication/Notifications/` covering FR-096 and FR-100–FR-101.
- [x] T015 Implement startup validation for unique installed adapter, policy, grant-source, and client identifiers plus exact callback/origin/logout registrations in `src/modules/Elsa.ExternalAuthentication/Validation/ExternalAuthenticationOptionsValidator.cs` covering FR-017–FR-019, FR-036, FR-075, and FR-077–FR-078.
- [x] T016 Implement configuration-owned connection loading, validation, immutable IDs, and canonical material revisions in `src/modules/Elsa.ExternalAuthentication/Providers/ConfigurationIdentityProviderConnectionSource.cs` and `Services/ConnectionRevisionCalculator.cs` covering FR-001–FR-003, FR-006–FR-009, FR-020, FR-039, and FR-098.
- [x] T017 Implement the merged effective connection registry with source precedence, collision rejection, shadow diagnostics, tenant isolation, deterministic ordering, and version barriers in `src/modules/Elsa.ExternalAuthentication/Services/DefaultIdentityProviderConnectionRegistry.cs` covering FR-004–FR-010, FR-016, FR-041, and FR-071.
- [x] T018 [P] Implement atomic in-memory broker transaction, completion grant, session, preview, observation, and version-barrier stores for single-node use in `src/modules/Elsa.ExternalAuthentication/Stores/InMemory/` covering FR-034–FR-046 and FR-086–FR-090.
- [x] T019 Refactor reusable Elsa access/refresh token construction behind `IElsaTokenService` while preserving `IAccessTokenIssuer` and existing token contracts in `src/modules/Elsa.Identity/Contracts/IElsaTokenService.cs` and `src/modules/Elsa.Identity/Services/DefaultElsaTokenService.cs` covering FR-034, FR-045, FR-060, and FR-073.
- [x] T020 Make local credentials optional without placeholder hashes and preserve indistinguishable invalid-login behavior in `src/modules/Elsa.Identity/Entities/User.cs` and `src/modules/Elsa.Identity/Services/DefaultUserCredentialsValidator.cs` covering FR-052–FR-053.
- [x] T021 Wire the protocol-neutral feature, shell feature, services, FastEndpoints groups, rate limiting, and Data Protection purposes in `src/modules/Elsa.ExternalAuthentication/Features/ExternalAuthenticationFeature.cs` and `ShellFeatures/ExternalAuthenticationShellFeature.cs`.
- [x] T022 [P] Add foundational unit tests for options validation, material revisions, registry merge/tenant rules, atomic single-use stores, redaction, and local credential compatibility in `test/unit/Elsa.ExternalAuthentication.UnitTests/Foundational/` and `test/unit/Elsa.Identity.UnitTests/ExternalAuthentication/`.
- [x] T023 [P] Add safe public error categories, correlation IDs, allowlisted local return paths, and shared response redaction helpers in `src/modules/Elsa.ExternalAuthentication/Services/BrokerErrorFactory.cs`, `Validation/ClientReturnPathValidator.cs`, and `Services/ExternalAuthenticationRedactor.cs` covering FR-078 and FR-094–FR-097.

**Checkpoint**: The broker has stable protocol-neutral contracts, secure configuration, source composition, atomic single-node primitives, and compatible Elsa credential issuance.

---

## Phase 3: User Story 1 - Sign In Through an External Provider (Priority: P1) — MVP

**Goal**: Let a user discover an enabled OpenID Connect connection, authenticate upstream, resolve or JIT-provision an Elsa user, and receive Elsa credentials through a PKCE-bound completion code.

**Independent Test**: Configure one connection and one client entirely in configuration, run discovery → authorize → provider callback → token exchange against the deterministic fake provider, and verify one Elsa user/link/session plus usable Elsa permissions.

### Tests for User Story 1

- [x] T024 [P] [US1] Add OpenID Connect adapter conformance and validation tests for code flow, state, nonce, signature, issuer, audience/authorized-party, expiry, callback errors, optional upstream PKCE, and normalized claims in `test/unit/Elsa.ExternalAuthentication.UnitTests/OpenIdConnect/OpenIdConnectAdapterTests.cs` covering FR-022–FR-025 and SC-001.
- [x] T025 [P] [US1] Add broker endpoint contract tests for discovery, initiation, callback, local authorize, code exchange, and logout in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Broker/BrokerContractTests.cs` covering FR-032–FR-048, FR-066–FR-073, and SC-001.
- [x] T026 [P] [US1] Add replay, exact-callback, PKCE, revision-change, disabled/archive, tenant enumeration, and redirect-allowlist security tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Security/BrokerSecurityTests.cs` covering FR-033–FR-041, FR-046, FR-077–FR-078, FR-094–FR-095, and SC-007/SC-011.
- [x] T027 [P] [US1] Add credential-less JIT concurrency and local-login indistinguishability tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Identity/JustInTimeProvisioningTests.cs` covering FR-049–FR-057 and SC-009.

### Implementation for User Story 1

- [x] T028 [P] [US1] Implement the OpenID Connect adapter descriptor, versioned settings, discovery/manual trust validation, and adapter registration in `src/modules/Elsa.ExternalAuthentication.OpenIdConnect/` covering FR-018–FR-024 and FR-029.
- [x] T029 [US1] Implement hardened provider authorization and callback processing with maintained protocol primitives and normalized claim projection in `src/modules/Elsa.ExternalAuthentication.OpenIdConnect/Services/OpenIdConnectExternalAuthenticationAdapter.cs` covering FR-022–FR-024 and FR-092–FR-099.
- [x] T030 [P] [US1] Implement reject and JIT unlinked-identity policies in `src/modules/Elsa.ExternalAuthentication/Policies/RejectUnlinkedIdentityPolicy.cs` and `Policies/CreateUserUnlinkedIdentityPolicy.cs` covering FR-049–FR-058.
- [x] T031 [US1] Implement atomic link resolution and credential-less create-link-or-get-existing provisioning in `src/modules/Elsa.ExternalAuthentication/Services/DefaultExternalIdentityResolver.cs` covering FR-049–FR-057.
- [x] T032 [US1] Implement Login Method discovery and external/local initiation endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/GetLoginMethods.cs`, `AuthorizeExternal.cs`, and `AuthorizeLocal.cs` covering FR-016, FR-032, FR-035–FR-041, and FR-066–FR-073.
- [x] T033 [US1] Implement provider callback processing and single-use PKCE-bound completion grants in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/HandleCallback.cs` and `Services/ExternalAuthenticationBroker.cs` covering FR-032–FR-041 and FR-049–FR-057.
- [x] T034 [US1] Implement authorization-code exchange, rotating external refresh, session checks, and additive local broker completion in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/ExchangeToken.cs` covering FR-034–FR-046 and FR-072–FR-076.
- [x] T035 [US1] Implement Elsa logout plus Disabled/UserChoice/Always upstream logout behavior in `src/modules/Elsa.ExternalAuthentication/Endpoints/Broker/Logout.cs` covering FR-047–FR-048 and FR-077.

**Checkpoint**: Configuration-first external and opt-in brokered local sign-in work end to end without persisted administration.

---

## Phase 4: User Story 2 - Manage Persisted Connections (Priority: P1)

**Goal**: Let authorized administrators create, inspect, edit, enable, disable, archive, restore, and delete eligible database-owned connections without restarting Elsa.

**Independent Test**: Create a disabled draft through the API, complete its fields and Secret Bindings, enable it, observe it in discovery immediately on a second node, update with ETags, archive/restore it, and verify configuration-owned entries remain read-only.

### Tests for User Story 2

- [x] T036 [P] [US2] Add CRUD, draft/enable, archive/restore, source-ownership, collision, ETag, and stale-registry contract tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Connections/ConnectionManagementTests.cs` covering FR-001–FR-016 and SC-002/SC-012.
- [x] T037 [P] [US2] Add EF persistence and migration tests for all entities, unique indexes, concurrency tokens, atomic JIT/link transactions, and authoritative registry versions in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Persistence/ExternalAuthenticationPersistenceTests.cs` covering FR-011–FR-012, FR-040–FR-041, FR-055, and SC-006.
- [x] T038 [P] [US2] Add Studio connection list/editor component tests for ownership, lifecycle, validation, secret configured-state, unsafe-setting warnings, and allowed actions in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Connections/ConnectionEditorTests.cs` covering FR-014, FR-026–FR-030, FR-082–FR-085, and SC-002.

### Implementation for User Story 2

- [x] T039 [P] [US2] Add persisted connection, policy/grant selections, claim projection, link, client, broker transaction, grant, session, observation, and preview entities/configurations to `src/modules/Elsa.Persistence.EFCore/Modules/Identity/DbContext.cs` and `Modules/ExternalAuthentication/Configurations.cs` covering the `data-model.md` persistence model.
- [x] T040 [US2] Implement EF connection, registry-version, atomic state, session, observation, preview, and atomic identity-link stores in `src/modules/Elsa.Persistence.EFCore/Modules/ExternalAuthentication/` covering FR-003, FR-011–FR-012, FR-040–FR-041, FR-055, and FR-090.
- [x] T041 [P] [US2] Generate Identity migrations and snapshots for SQLite, SQL Server, PostgreSQL, MySQL, and Oracle in `src/modules/Elsa.Persistence.EFCore.{Sqlite,SqlServer,PostgreSql,MySql,Oracle}/Migrations/Identity/`.
- [x] T042 [US2] Implement create/read/update/enable/disable/archive/restore/delete endpoints with ETags, source ownership, collision semantics, validation, and security notifications in `src/modules/Elsa.ExternalAuthentication/Endpoints/Connections/` covering FR-001–FR-015, FR-082, FR-085, and FR-100.
- [x] T043 [P] [US2] Implement descriptor, permission-descriptor, and policy/grant-source catalog endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/Descriptors/` covering FR-017–FR-019 and FR-064.
- [x] T044 [P] [US2] Implement the optional Elsa Secrets resolver with generation fingerprints and no-reveal replacement/removal semantics in `src/modules/Elsa.ExternalAuthentication.Secrets/Services/ElsaSecretBindingResolver.cs` covering FR-026–FR-028 and FR-098.
- [x] T045 [P] [US2] Add typed External Authentication resources and Refit clients in `src/clients/Elsa.Api.Client/Resources/ExternalAuthentication/` for every management and descriptor endpoint in `contracts/rest-api.md`.
- [x] T046 [US2] Implement the Studio Security menu and paginated connection list with source, scope, enabled/valid/test states, shadowing, and caller-authorized actions in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Menu/ExternalAuthenticationMenu.cs` and `Pages/Connections/Index.razor` covering FR-080, FR-082–FR-085.
- [x] T047 [US2] Implement the schema-driven Studio connection editor, lifecycle dialogs, Secret Binding controls, concurrency recovery, and unsafe trust confirmation in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Pages/Connections/Edit.razor` and `Components/ConnectionEditor/` covering FR-014, FR-018–FR-021, FR-026–FR-030, and FR-083.

**Checkpoint**: Persisted connection administration is complete and cross-node discovery reflects committed mutations.

---

## Phase 5: User Story 3 - Preserve Elsa Authorization (Priority: P1)

**Goal**: Resolve every authenticated external identity to an Elsa user and compose only Elsa-authorized permissions from explicit, bounded grant sources.

**Independent Test**: Sign in with mapped and unmapped external claims, inspect Preview Sign-in provenance, modify Elsa roles, refresh the external session, and verify unmapped/unauthorized permissions never appear while current Elsa-owned grants do.

### Tests for User Story 3

- [x] T048 [P] [US3] Add permission pipeline unit tests for source composition, deterministic deduplication, provenance, unknown descriptors, allow/deny boundaries, and unmapped claims in `test/unit/Elsa.ExternalAuthentication.UnitTests/Permissions/PermissionGrantPipelineTests.cs` covering FR-060–FR-065 and SC-008.
- [x] T049 [P] [US3] Add delegation authorization tests proving ordinary administrators cannot grant permissions they lack while unrestricted delegates remain deployment-bounded in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Permissions/PermissionDelegationTests.cs` covering FR-063–FR-064 and SC-008.
- [x] T050 [P] [US3] Add external refresh tests proving snapshots remain bounded while current Elsa user/role grants are reevaluated in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Sessions/ExternalRefreshPermissionTests.cs` covering FR-042–FR-045 and FR-060–FR-065.

### Implementation for User Story 3

- [x] T051 [P] [US3] Implement Elsa user/role and explicit external claim/group Permission Grant Sources in `src/modules/Elsa.ExternalAuthentication/Permissions/` covering FR-060–FR-065.
- [x] T052 [US3] Implement ordered grant-source composition, permission provenance, deployment boundaries, and delegation checks in `src/modules/Elsa.ExternalAuthentication/Services/DefaultPermissionGrantResolver.cs` covering FR-060–FR-064.
- [x] T053 [P] [US3] Add the optional module-contributed permission descriptor provider/registry in `src/modules/Elsa.ExternalAuthentication/Services/DefaultPermissionDescriptorRegistry.cs` covering FR-064.
- [x] T054 [US3] Integrate resolved permission grants and provenance with Elsa token issuance and external-session snapshots in `src/modules/Elsa.ExternalAuthentication/Services/ExternalAuthenticationBroker.cs` covering FR-042–FR-045 and FR-060–FR-065.
- [x] T055 [P] [US3] Implement permission mapping and boundary controls in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Components/PermissionMappings/` covering FR-061–FR-064.
- [x] T056 [US3] Implement Preview Sign-in permission provenance and descriptor warnings in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Components/Preview/PermissionPreview.razor` covering FR-064 and FR-086–FR-088.
- [x] T057 [US3] Add permission preview and delegation component tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Permissions/PermissionMappingTests.cs` covering FR-061–FR-064 and SC-008.

**Checkpoint**: Elsa remains the sole issuer of authoritative permission claims, with explicit external mappings and visible provenance.

---

## Phase 6: User Story 4 - Use the Same Broker from Studio Server and WebAssembly (Priority: P1)

**Goal**: Provide one accessible login chooser and management module across Studio Server and WebAssembly while honoring each host's trust boundary.

**Independent Test**: Run the same configured connection from both Studio hosts; verify server-side confidential exchange/cookies, WebAssembly public PKCE exchange/memory-only tokens, exact origins, chooser fallback, and opt-in persistence warnings.

### Tests for User Story 4

- [x] T058 [P] [US4] Add shared chooser component tests for deterministic ordering, local/external methods, automatic-default escape, error fallback, unavailable state, and trusted icons in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Login/LoginChooserTests.cs` covering FR-066–FR-071 and FR-084/SC-015.
- [x] T059 [P] [US4] Add Blazor Server integration tests for confidential host exchange, HTTP-only session, server-held refresh, return paths, and logout in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/BlazorServer/ServerBrokerAuthenticationTests.cs` covering FR-074 and FR-077–FR-078.
- [x] T060 [P] [US4] Add WebAssembly Playwright tests for mandatory PKCE, exact origin, memory-only default, optional session/durable warnings, reload/tab behavior, refresh rotation, and logout in `/Users/sipke/Projects/Elsa/elsa-studio/tests/browser/ExternalAuthentication/broker-authentication.spec.ts` covering FR-075–FR-078 and SC-001/SC-011.

### Implementation for User Story 4

- [x] T061 [P] [US4] Implement typed discovery/exchange/logout clients and authentication state abstractions in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Client/` and `Services/`.
- [x] T062 [US4] Implement the accessible Login Method chooser, default redirect loop guard, explicit chooser escape, and safe unavailable/error states in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Pages/Login.razor` covering FR-066–FR-071 and FR-084.
- [x] T063 [P] [US4] Implement Blazor Server challenge/callback/logout controllers and confidential code exchange in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorServer/Controllers/ExternalAuthenticationController.cs` covering FR-034–FR-035 and FR-074/FR-077–FR-078.
- [x] T064 [US4] Implement secure HTTP-only Studio Server sessions, server-side refresh storage, and Elsa API authorization in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorServer/Services/ServerExternalAuthenticationStateProvider.cs` covering FR-043–FR-048 and FR-074.
- [x] T065 [P] [US4] Implement WebAssembly PKCE/state generation, callback exchange, in-memory default token accessor, and rotating refresh in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/Services/` covering FR-035 and FR-075–FR-078.
- [x] T066 [US4] Implement explicit tab-session and durable browser storage options with security warnings in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/Extensions/ServiceCollectionExtensions.cs` covering FR-076.
- [x] T067 [US4] Register the shared, Server, and WebAssembly features in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/ExternalAuthenticationFeature.cs` and the host-specific feature classes covering FR-074–FR-076 and FR-107.
- [x] T068 [US4] Integrate the deployment-selected broker mode with `/Users/sipke/Projects/Elsa/elsa-studio/src/hosts/Elsa.Studio.Host.Server/Program.cs` and `/Users/sipke/Projects/Elsa/elsa-studio/src/hosts/Elsa.Studio.Host.Wasm/Program.cs` without enabling it by default covering FR-104–FR-107.

**Checkpoint**: Both Studio hosts use the same broker contract with host-appropriate credential handling.

---

## Phase 7: User Story 5 - Operate Connections Safely (Priority: P2)

**Goal**: Give administrators safe testing, Preview Sign-in, session revocation, lockout recovery, and redacted operational signals without adding continuous health or audit storage.

**Independent Test**: Test and preview a draft, verify stale observations after material changes, prove Preview creates no account/session/token, disable the final normal method only through the guarded override path, and revoke an external session.

### Tests for User Story 5

- [x] T069 [P] [US5] Add test/observation and stale-revision contract tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Operations/ConnectionTestTests.cs` covering FR-013, FR-015, FR-089–FR-091.
- [x] T070 [P] [US5] Add Preview Sign-in isolation, authorization, one-time result, redaction, and no-side-effect tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Operations/PreviewSignInTests.cs` covering FR-086–FR-088 and SC-010.
- [x] T071 [P] [US5] Add outbound HTTP egress, SSRF, DNS rebinding, timeout, redirect, size, proxy, and exception conformance tests in `test/unit/Elsa.ExternalAuthentication.UnitTests/Security/OutboundProviderHttpTests.cs` covering FR-092–FR-099 and SC-011.
- [x] T072 [P] [US5] Add notification exhaustiveness and redaction tests in `test/unit/Elsa.ExternalAuthentication.UnitTests/Notifications/SecurityNotificationTests.cs` covering FR-096, FR-100–FR-101, and SC-004/SC-014.
- [x] T073 [P] [US5] Add final-login-path guard, Break-glass invisibility, session revocation, and connection-disable tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Operations/RecoveryAndRevocationTests.cs` covering FR-046, FR-102–FR-103, and SC-007.

### Implementation for User Story 5

- [x] T074 [P] [US5] Implement on-demand connection testing and shared latest redacted observation endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/Connections/TestConnection.cs` and `Services/ConnectionTestService.cs` covering FR-013, FR-015, FR-089–FR-090.
- [x] T075 [P] [US5] Implement the separately tagged opt-in ASP.NET Core health-check bridge in `src/modules/Elsa.ExternalAuthentication/Services/ExternalAuthenticationHealthCheck.cs` covering FR-091.
- [x] T076 [US5] Implement administrator-bound preview initiation/callback/result endpoints and one-time stores in `src/modules/Elsa.ExternalAuthentication/Endpoints/Previews/` covering FR-086–FR-088.
- [x] T077 [P] [US5] Implement hardened outbound provider HTTP handling and configurable secure egress policies in `src/modules/Elsa.ExternalAuthentication/Services/ProviderHttpClientFactory.cs` and `Validation/OutboundDestinationValidator.cs` covering FR-092–FR-099.
- [x] T078 [P] [US5] Implement external-session listing and revocation endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/Sessions/` covering FR-042–FR-046, FR-085, and FR-100.
- [x] T079 [US5] Implement the final-login-path guard, privileged confirmation, and Break-glass reachability contract in `src/modules/Elsa.ExternalAuthentication/Services/FinalLoginPathGuard.cs` covering FR-102–FR-103.
- [x] T080 [US5] Add Studio Test, Preview Sign-in, stale observation, session list/revoke, lockout warning, and recovery UI in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Components/Operations/` and `Pages/Sessions/Index.razor` covering FR-083, FR-086–FR-090, and FR-102–FR-103.
- [x] T081 [US5] Publish redacted typed notifications from every sign-in outcome and privileged connection, policy, secret, test, preview, link, and session operation through `src/modules/Elsa.ExternalAuthentication/Services/ExternalAuthenticationSecurityNotifier.cs` covering FR-096 and FR-100–FR-101.

**Checkpoint**: Operational testing, recovery, preview, revocation, and notification flows are safe and observable without health history or a built-in audit store.

---

## Phase 8: User Story 6 - Extend Providers and Policies (Priority: P2)

**Goal**: Prove deployed adapters, policies, grant sources, descriptor-driven forms, custom editors, and settings migration can extend the feature without changing the connection schema.

**Independent Test**: Install a conformance adapter with unique versioned settings and a custom policy, configure both through the generic UI, migrate an old settings version, and authenticate without broker schema changes.

### Tests for User Story 6

- [x] T082 [P] [US6] Add adapter/policy/grant-source registry conformance tests with duplicate IDs, deployment allowlists, version migration, descriptor completeness, and unsupported adapters in `test/unit/Elsa.ExternalAuthentication.UnitTests/Extensibility/ExtensionConformanceTests.cs` covering FR-017–FR-025 and SC-003.
- [x] T083 [P] [US6] Add generic descriptor-editor and optional custom-editor component tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Extensibility/DescriptorEditorTests.cs` covering FR-018–FR-021 and SC-003.

### Implementation for User Story 6

- [x] T084 [P] [US6] Implement immutable installed adapter, policy, and grant-source registries with stable identifiers and deployment allowlists in `src/modules/Elsa.ExternalAuthentication/Services/ExtensionRegistries.cs` covering FR-017.
- [x] T085 [P] [US6] Implement descriptor schema validation, conditional visibility, capability metadata, UI hints, secret metadata, and custom-editor contract versions in `src/modules/Elsa.ExternalAuthentication/Services/ExtensionDescriptorValidator.cs` covering FR-018–FR-019.
- [x] T086 [US6] Implement adapter-owned opaque settings compatibility and migration orchestration in `src/modules/Elsa.ExternalAuthentication/Services/AdapterSettingsMigrationService.cs` covering FR-020–FR-021.
- [x] T087 [P] [US6] Add a test-only conformance adapter and custom policy/grant source in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Fixtures/ConformanceExtensions/` covering FR-025 and SC-003.
- [x] T088 [US6] Implement the generic Studio descriptor form renderer with complete fallback controls in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Components/Descriptors/DescriptorForm.razor` covering FR-018–FR-021.
- [x] T089 [US6] Implement the versioned optional custom-editor registry and safe fallback in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Services/CustomConnectionEditorRegistry.cs` covering FR-019.

**Checkpoint**: New trusted provider and policy extensions require deployed code but no broker schema or generic Studio changes.

---

## Phase 9: User Story 7 - Administer External Identity Links (Priority: P2)

**Goal**: Let authorized administrators inspect, prelink, and unlink external identities with strict tenant and uniqueness enforcement.

**Independent Test**: Prelink an external tuple to a tenant user, authenticate into that user, reject a cross-tenant target and concurrent duplicate link, then unlink and observe the configured unlinked policy on the next sign-in.

### Tests for User Story 7

- [x] T090 [P] [US7] Add link list/prelink/unlink, tenant isolation, uniqueness convergence, archived-connection retention, and policy fallback integration tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Links/ExternalIdentityLinkTests.cs` covering FR-011 and FR-049–FR-059 and SC-005.
- [x] T091 [P] [US7] Add tenant-scoped minimal user lookup authorization and data-minimization tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Links/UserLookupTests.cs` covering FR-056, FR-081–FR-082.
- [x] T092 [P] [US7] Add Studio link administration component tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Links/ExternalIdentityLinksTests.cs` covering FR-059, FR-080–FR-085.

### Implementation for User Story 7

- [x] T093 [P] [US7] Implement tenant-scoped paginated external-link list and detail endpoints in `src/modules/Elsa.ExternalAuthentication/Endpoints/IdentityLinks/GetIdentityLinks.cs` covering FR-050, FR-056, FR-059, and FR-082.
- [x] T094 [P] [US7] Implement permission-guarded minimal tenant user lookup in `src/modules/Elsa.ExternalAuthentication/Endpoints/IdentityLinks/FindUsers.cs` covering FR-056 and FR-081–FR-082.
- [x] T095 [US7] Implement transactional prelink and unlink endpoints with tuple uniqueness, tenant matching, archived identity retention, and notifications in `src/modules/Elsa.ExternalAuthentication/Endpoints/IdentityLinks/` covering FR-011, FR-050–FR-051, FR-055–FR-059, FR-085, and FR-100.
- [x] T096 [P] [US7] Add external-link resources and minimal user lookup to `src/clients/Elsa.Api.Client/Resources/ExternalAuthentication/IdentityLinks/`.
- [x] T097 [US7] Implement Studio External Identity Links list, filters, prelink user picker, tuple display, and unlink confirmation in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Pages/IdentityLinks/` covering FR-059 and FR-080–FR-085.
- [x] T098 [US7] Integrate link-management permission visibility with the Studio Security menu in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/Menu/ExternalAuthenticationMenu.cs` covering FR-082–FR-085.

**Checkpoint**: Link administration is explicit, tenant-safe, concurrency-safe, and independent of mutable profile attributes.

---

## Phase 10: User Story 8 - Migrate Existing Direct OpenID Connect Deployments (Priority: P3)

**Goal**: Preserve direct Studio OpenID Connect while making broker mode an explicit, validated alternative with actionable migration guidance.

**Independent Test**: Start each Studio host in Direct, Brokered, and invalid mixed modes; prove Direct behavior is unchanged, Brokered behavior works, and mixed configuration fails startup with remediation guidance.

### Tests for User Story 8

- [x] T099 [P] [US8] Add Studio startup matrix tests for Direct, Brokered, local-only, and ambiguous modes in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication.Tests/Compatibility/AuthenticationModeTests.cs` covering FR-104–FR-107 and SC-013.
- [x] T100 [P] [US8] Add Core regression tests for existing `/identity/login` and `/identity/refresh-token` contracts in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Compatibility/LegacyIdentityEndpointTests.cs` covering FR-045 and FR-073.

### Implementation for User Story 8

- [x] T101 [US8] Add explicit authentication-mode options and fail-fast mutual-exclusion validation in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Authentication.Abstractions/` covering FR-104–FR-105.
- [x] T102 [US8] Preserve existing Direct OpenID Connect registrations and select Brokered mode only when configured in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Authentication.OpenIdConnect.BlazorServer/Extensions/ServiceCollectionExtensions.cs` and `Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/Extensions/ServiceCollectionExtensions.cs` covering FR-104–FR-105.
- [x] T103 [P] [US8] Document direct-to-broker setting mappings, unchanged secret ownership, explicit mode switch, rollback, and both host variants in `docs/migrations/external-authentication.md` and `/Users/sipke/Projects/Elsa/elsa-studio/docs/migrations/external-authentication.md` covering FR-104–FR-107.
- [x] T104 [US8] Add configuration-owned migration examples for Server and WebAssembly to `specs/012-external-authentication/quickstart.md` covering FR-106–FR-108.
- [x] T105 [US8] Verify and document unchanged direct-login behavior and additive broker-local behavior in `src/modules/Elsa.Identity/README.md` and `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.ExternalAuthentication/README.md` covering FR-073 and FR-104–FR-108.

**Checkpoint**: Existing deployments remain stable until an explicit, validated migration.

---

## Phase 11: Polish and Cross-Cutting Verification

**Purpose**: Validate the combined delivery against its security, accessibility, compatibility, scale, and documentation gates.

- [x] T106 [P] Add cross-surface leakage contract tests for responses, redirects, logs, notifications, tests, previews, health details, and Studio models in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Security/SensitiveDataLeakageTests.cs` covering FR-028, FR-033, FR-065, FR-096–FR-098, and SC-004.
- [x] T107 [P] Add multi-node initiation/callback/exchange/refresh and cross-node mutation consistency tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Distributed/MultiNodeBrokerTests.cs` covering FR-038–FR-046, FR-090, and SC-006/SC-007.
- [x] T108 [P] Add tenant discovery/link fuzz tests and 10,000-connection registry paging tests in `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Scale/TenantAndRegistryScaleTests.cs` covering FR-009–FR-016, FR-050, and SC-005.
- [x] T109 [P] Add component accessibility tests and Playwright axe/keyboard/screen-reader-semantics/trusted-asset checks in `/Users/sipke/Projects/Elsa/elsa-studio/tests/browser/ExternalAuthentication/accessibility.spec.ts` covering FR-067–FR-069, FR-083–FR-084, and SC-015.
- [x] T110 [P] Add benchmark/load scenarios for discovery, management paging, and broker overhead in `test/performance/Elsa.Workflows.PerformanceTests/ExternalAuthentication/ExternalAuthenticationBenchmarks.cs` covering the performance goals in `plan.md`.
- [x] T111 Review all External Authentication XML docs, README files, option defaults, permission descriptions, endpoint summaries, and configuration examples in `src/modules/Elsa.ExternalAuthentication/`, `src/modules/Elsa.ExternalAuthentication.OpenIdConnect/`, `src/modules/Elsa.ExternalAuthentication.Secrets/`, and `specs/012-external-authentication/quickstart.md`.
- [x] T112 Run `dotnet test test/unit/Elsa.ExternalAuthentication.UnitTests/Elsa.ExternalAuthentication.UnitTests.csproj` and `dotnet test test/unit/Elsa.Identity.UnitTests/Elsa.Identity.UnitTests.csproj` from the `elsa-core` repository.
- [x] T113 Run `dotnet test test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj` from the `elsa-core` repository.
- [x] T114 Run `dotnet build Elsa.sln` from the `elsa-core` repository and resolve new warnings or errors without unrelated cleanup.
- [x] T115 Run `dotnet test src/modules/Elsa.Studio.ExternalAuthentication.Tests/Elsa.Studio.ExternalAuthentication.Tests.csproj` and the External Authentication Playwright suite from `/Users/sipke/Projects/Elsa/elsa-studio/`.
- [x] T116 Run `dotnet build Elsa.Studio.sln` from `/Users/sipke/Projects/Elsa/elsa-studio/` and validate both quickstart host configurations.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)** has no dependencies.
- **Foundational (Phase 2)** depends on Setup and blocks all user stories.
- **US1 (Phase 3)** is the configuration-first MVP.
- **US2 (Phase 4)** depends on the foundational source/store contracts; it can proceed alongside US1 after those contracts stabilize.
- **US3 (Phase 5)** depends on token and session contracts from Foundation and integrates with US1 at T054.
- **US4 (Phase 6)** depends on the public broker contract from US1 but its shared UI and host-state services can begin against the frozen REST contract.
- **US5 (Phase 7)** depends on Foundation; preview callback integration depends on the adapter path from US1.
- **US6 (Phase 8)** depends only on the foundational extension contracts and can proceed in parallel with US1–US5.
- **US7 (Phase 9)** depends on the atomic link contract from Foundation and integrates with the resolver from US1.
- **US8 (Phase 10)** depends on the Studio broker mode from US4.
- **Polish (Phase 11)** depends on every selected story.

### User Story Completion Order

```text
Setup → Foundation ┬→ US1 ─┬→ US3
                   │       ├→ US4 → US8
                   │       ├→ US5
                   │       └→ US7
                   ├→ US2
                   └→ US6

US1–US8 → Polish and Cross-Cutting Verification
```

### Within Each User Story

- Write the listed tests first and confirm that they fail for the intended missing behavior.
- Implement models and infrastructure before services, services before endpoints/components, and endpoints before end-to-end verification.
- Complete the independent test before treating a story as done.

## Parallel Execution Examples

- **US1**: T024–T027 can run together; T028 and T030 can run together before T029/T031–T035.
- **US2**: T036–T038 can run together; T041, T043–T045 can run in parallel after T039.
- **US3**: T048–T050 can run together; T051 and T053 can run together before T052/T054.
- **US4**: T058–T060 can run together; T061, T063, and T065 can run together against the REST contract.
- **US5**: T069–T073 can run together; T074, T075, T077, and T078 can run together.
- **US6**: T082–T083 can run together; T084, T085, and T087 can run together.
- **US7**: T090–T092 can run together; T093, T094, and T096 can run together.
- **US8**: T099–T100 and T103 can run together before final mode integration.

## Requirements Coverage

| Requirement range | Primary tasks |
| --- | --- |
| FR-001–FR-016 | T010, T016–T017, T036–T042, T108 |
| FR-017–FR-031 | T011, T015, T024, T028–T029, T043–T047, T082–T089 |
| FR-032–FR-048 | T010, T018–T019, T025–T026, T032–T035, T050, T054, T059–T065, T073, T078, T107 |
| FR-049–FR-065 | T011, T027, T030–T031, T048–T057, T090–T098 |
| FR-066–FR-084 | T012, T023, T025–T026, T032, T058–T068, T091–T092, T097–T098, T109 |
| FR-085–FR-103 | T012–T014, T023, T026, T038, T042, T047, T069–T081, T106 |
| FR-104–FR-108 | T068, T099–T105 |
| SC-001–SC-003 | T024–T025, T036–T038, T082–T089 |
| SC-004–SC-007 | T026, T037, T072–T073, T090, T106–T108 |
| SC-008–SC-011 | T027, T048–T057, T060, T070–T073 |
| SC-012–SC-015 | T036–T037, T072, T081, T099–T105, T109 |

## Implementation Strategy

### MVP First

1. Complete Setup and Foundation.
2. Complete US1 with configuration-owned OpenID Connect and in-memory single-node stores.
3. Demonstrate discovery, external callback, JIT/link resolution, permission issuance, code exchange, refresh, and logout independently.

### Incremental Delivery

1. Add US2 for persisted management and cross-node state.
2. Add US3 and US4 for full Elsa authorization and both Studio hosts.
3. Add US5–US7 for operational safety, extension conformance, and link administration.
4. Add US8 compatibility guidance and finish the cross-cutting gates.

## Notes

- `[P]` means different files and no dependency on an incomplete task in the same phase.
- Configuration-first/single-node deployments may use in-memory state; multi-node deployments require EF state plus shared Data Protection.
- No task adds a continuous health monitor, health history, audit database, social-provider shortcuts, end-user self-linking, or a general OAuth authorization server.
- Commit after each coherent implementation slice rather than mechanically after every checkbox.
