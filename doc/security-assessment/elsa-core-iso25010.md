# Elsa Workflows Core — ISO 25010 quality assessment

| Field | Value |
|---|---|
| **Repository / project** | elsa-workflows/elsa-core |
| **Git ref** | release/3.7.0 |
| **Version** | 3.7.0 |
| **Assessment date (UTC)** | 2026-05-20T00:00:00Z |
| **Assessment method** | Static code analysis, Documentation review, ISO 25010:2023 framework |
| **Model and tools** | Claude Sonnet 4.6 · Static code analysis · ISO 25010:2023 |
| **Assessment scope** | All 50 source projects in src/ — primary focus on core engine, security, resilience modules |
| **Related documents** | elsa-core-profile.md, elsa-core-architecture-patterns.md, elsa-core-software-quality.md |

> ⚠️ **AI-assisted assessment — human review required**
>
> This document was produced by an AI model (Claude Sonnet 4.6) using static code analysis
> and the ISO 25010:2023 framework. AI assessments are based on pattern recognition
> over source files and documentation. They may contain incorrect assumptions, missed
> context, or findings that do not apply to your specific operational environment.
>
> **This document is a working draft — not a final approved report.**
> Human engineers and architects must complete all "Human completion required" checklists
> and all verdict fields before this document can be treated as approved.
> The final approved report is this document with all verdict fields completed,
> signed off by a named reviewer.

---

> **How to use this document**
>
> 1. Review each "What Claude assesses" block — correct any wrong assumptions in the Notes field
> 2. Complete each "Human completion required" checklist — gather evidence from CONFIG, RUNTIME, and OPS sources
> 3. Fill in each Verdict field based on the combined evidence (Claude's + yours)
> 4. Where Claude confidence is Low or Very low, weight your own evidence more heavily
> 5. Sign off the document and record the reviewer name and date on each verdict

---

## 1. Functional suitability

### 1.1 Functional completeness

**Evidence sources:** `CODE` `DOCS`
**Claude confidence:** High — README feature list cross-checked against interface contracts and activity inventory

**What Claude assesses (code-level):**
- The activity library in `src/modules/Elsa.Workflows.Core/Activities/` contains a broad primitive set: `Sequence`, `Flowchart`, `Fork`, `Parallel`, `ParallelForEach`, `ForEach`, `For`, `While`, `If`, `Switch`, `Break`, `Complete`, `End`, `Fault`, `Finish`, `SetVariable`, `Correlate`, `DynamicActivity`, `Inline`, and `Workflow` itself. The primitives cover sequential, branching, looping, parallel, and fault-handling patterns.
- The README explicitly lists built-in activities for HTTP calls, email, scheduling, messaging, and PDF generation — modules for HTTP (`Elsa.Http`), scheduling (`Elsa.Scheduling`), and scripting (C#, JavaScript, Python, Liquid) are present as separate module projects.
- Workflow versioning is present through `IWorkflowDefinitionPublisher.PublishAsync / RetractAsync / RevertVersionAsync` and `GetDraftAsync/SaveDraftAsync` — covering the full definition lifecycle.
- Persistence is covered by EF Core providers for SQLite, SQL Server, PostgreSQL, MySQL, and Oracle. The `IWorkflowDefinitionStore` / `IWorkflowInstanceStore` abstraction permits additional providers.
- Workflow alteration (`Elsa.Alterations`, `Elsa.Alterations.Core`) enables in-flight workflow modification.
- `IWorkflowRuntime`, `IWorkflowClient`, `IWorkflowDispatcher` cover runtime start, resume, and dispatch surface.
- In-memory stores exist for all major entities — meaning fully functional operation without external persistence for testing/embedded scenarios.
- The README states Elsa supports .NET 6 and beyond; `Directory.Build.props` targets net8.0;net9.0;net10.0, so .NET 6/7 are no longer in scope for this branch — assumption: the README text predates this release line.

**Human completion required:**
- [ ] Verify that all features listed in the README are actually enabled/shipped in the v3.7.0 NuGet packages — some may be optional add-ons not in this repo (DOCS)
- [ ] Confirm that claimed MongoDB/Dapper persistence providers are shipped or externally maintained — no Dapper or Mongo .csproj found in this repository at this ref (DESIGN)
- [ ] Validate that workflow versioning (publish/retract/revert) round-trips correctly through all supported persistence providers (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 1.2 Functional correctness

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Medium — pipeline structure and tests suggest correctness intent; behavioural correctness requires runtime execution

**What Claude assesses (code-level):**
- The activity execution pipeline (`ActivityExecutionPipeline`, `WorkflowExecutionPipeline`) is well-structured and testable via `IActivityExecutionMiddleware`. Incorrect execution order would be visible in the pipeline builder tests.
- `ValidatingWorkflowDispatcher` decorates the base dispatcher and validates channel configuration before dispatch — a correctness guard at the dispatch boundary.
- `WorkflowHost.RunWorkflowAsync` explicitly guards against resuming workflows not in the `Running` state and logs a warning — preventing silent incorrect state transitions.
- 532 test methods across 12 unit and 9 integration projects provide regression coverage. Integration tests exercise actual workflow execution paths.
- `IIncidentStrategy` provides pluggable incident handling, meaning incorrect fault propagation would be customisable; however, the correctness of built-in strategies requires runtime validation.
- Typed `Input<T>` / `Output<T>` generic constraints (87 source files use them) reduce the risk of incorrect data type assumptions between activities.
- Assumption: test coverage percentage is unknown — 532 tests across ~2,416 source files is a ratio that could indicate under-coverage.

**Human completion required:**
- [ ] Run the full test suite and confirm all tests pass on the release/3.7.0 branch (RUNTIME)
- [ ] Review integration test coverage for the core workflow execution paths (fork/join, long-running suspend/resume, fault handling) (CODE)
- [ ] Identify whether mutation testing or property-based testing is used anywhere — not observed in the static scan (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 1.3 Functional appropriateness

**Evidence sources:** `DESIGN` `OPS`
**Claude confidence:** Low — appropriateness is a stakeholder judgement; code indicates design intent only

**What Claude assesses (code-level):**
- The design separates _definition_ (what a workflow does) from _instance_ (a running execution), which aligns with standard workflow engine patterns. The separation is consistent with established domain models (Windows Workflow Foundation, BPMN conceptually).
- The `IWorkflowActivationStrategy` abstraction allows per-workflow control over singleton, per-correlationId, or custom instantiation — indicating the design anticipated multi-tenancy and concurrency appropriateness concerns.
- The feature system (`IShellFeature` / `DependsOn`) allows adopters to select only the capabilities they need, which is appropriate for a library meant to be embedded in diverse host applications.
- Assumption: whether the current feature surface matches the documented roadmap (issue #3232 referenced in README) cannot be determined from static analysis.

**Human completion required:**
- [ ] Validate with product owners that the current API surface matches stakeholder requirements for v3.7.0 (DESIGN)
- [ ] Review GitHub issue #3232 (roadmap) and confirm in-scope items are delivered (DESIGN)
- [ ] Collect feedback from adopters on whether built-in activities cover their primary use cases without excessive custom activity development (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 2. Performance efficiency

### 2.1 Time behaviour

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Low — structural patterns suggest async-first design; actual latencies require measurement

**What Claude assesses (code-level):**
- All public async APIs use `Task<T>` or `ValueTask<T>` return types and accept `CancellationToken`. `ConfigureAwait.Fody` is enabled as a build weaver across all source projects, injecting `ConfigureAwait(false)` at compile time — eliminating a common ASP.NET throughput bottleneck without manual annotation.
- `IBookmarkQueueWorker`, `BackgroundStimulusDispatcher`, `BackgroundWorkflowDispatcher`, and `BookmarkQueueWorker` use queue-based background processing, decoupling dispatch latency from execution latency.
- The caching layer (`Elsa.Caching`) uses `IMemoryCache` with change-token invalidation, reducing repeated definition lookups. `CachingWorkflowRuntimeFeature` exists for the runtime layer.
- `BenchmarkDotNet` is used in `Elsa.Workflows.PerformanceTests/ConsoleActivitiesBenchmark` — a `WriteLine` workflow benchmark exists, showing performance measurement tooling is in place.
- No use of `Span<T>`, `Memory<T>`, or `ArrayPool<T>` was found in `Elsa.Workflows.Core` — high-volume serialization paths may allocate more than necessary. This is an assumption based on absence of these patterns; they may exist in lower-level helpers not scanned.

**Human completion required:**
- [ ] Run BenchmarkDotNet suite and record baseline throughput (workflow executions/sec) for common patterns (RUNTIME)
- [ ] Profile activity execution pipeline under load for GC pressure (RUNTIME)
- [ ] Confirm whether the single `ConsoleActivitiesBenchmark` is the only benchmark or whether more exist in the project (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 2.2 Resource utilisation

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Low — async patterns are sound; memory allocation profile requires runtime profiling

**What Claude assesses (code-level):**
- `ResilienceContextPool.Shared.Get()` and `.Return()` in `ResilientActivityInvoker` shows explicit context pooling for Polly resilience contexts — avoiding per-invocation allocation.
- `IAsyncDisposable` / `IDisposable` patterns appear in 28 files (e.g., `InstanceHeartbeatMonitorService`, `WorkflowHost`), reducing timer and scope leakage.
- The `BookmarkQueueWorker` uses an `RateLimitedFuncExtensions` pattern to prevent runaway processing — indicating awareness of CPU consumption boundaries.
- No explicit memory limits, object pool for `ActivityExecutionContext`, or `RecyclableMemoryStream` usage was observed in the core execution path — large workflow state serialization could produce high allocations per execution.
- Assumption: resource utilisation in production depends on concurrency settings and payload sizes, which are configurable and not assessable statically.

**Human completion required:**
- [ ] Profile memory allocations per workflow execution under representative load (RUNTIME)
- [ ] Measure CPU utilisation per 100 concurrent workflow executions under load test (RUNTIME)
- [ ] Review whether `ActivityExecutionContext` and `WorkflowExecutionContext` objects are pooled or newly allocated per run (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 2.3 Capacity

**Evidence sources:** `RUNTIME` `OPS`
**Claude confidence:** Very low — capacity is entirely a runtime/operational concern

**What Claude assesses (code-level):**
- The distributed runtime (`Elsa.Workflows.Runtime.Distributed`) with Medallion.Threading distributed locking indicates the design supports horizontal scaling — the lock provider abstracts node-local vs. distributed locking.
- `InstanceHeartbeatService` / `InstanceHeartbeatMonitorService` form a node-registry pattern that tracks active instances — a prerequisite for capacity-aware load distribution.
- No static configuration caps (max concurrent workflows, queue depth limits, database connection pool sizes) were observed in the source code defaults — these are expected to be configured per deployment.

**Human completion required:**
- [ ] Define and document capacity targets (concurrent workflow instances, peak throughput) (OPS)
- [ ] Run load tests to determine capacity ceiling per node configuration (RUNTIME)
- [ ] Verify that the bookmark queue depth and processing rate are observable and configurable for the target workload (CONFIG + RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 3. Compatibility

### 3.1 Co-existence

**Evidence sources:** `CONFIG` `RUNTIME`
**Claude confidence:** Low — co-existence depends on deployment topology, which cannot be fully assessed statically

**What Claude assesses (code-level):**
- The application (`Elsa.Server.Web`) configures `AllowedHosts: "*"` in appsettings — no host restriction in the default config, which is typical for library reference hosts.
- HTTP routes are configurable via `HttpActivityOptions` (`BasePath`, `ApiRoutePrefix: "elsa/api"`) — reducing the chance of path collision with other co-hosted applications.
- The EF Core schema defaults to `"Elsa"` (`ElsaDbContextBase.ElsaSchema`) with a configurable `SchemaName` option — allowing co-existence in shared databases without table name collisions.
- The feature module system (`IShellFeature`) means Elsa services are scoped into a `CShells` shell container — reducing DI registration conflicts with other application services. Assumption: the actual isolation boundary of `CShells` is not examined in detail here.
- Multi-tenant prefix routing (`TenantPrefixHttpEndpointRoutesProvider`) adds URL prefixes per tenant, which aids HTTP path co-existence in shared deployments.

**Human completion required:**
- [ ] Test Elsa co-hosted with common ASP.NET middleware stacks (authentication middleware ordering, SignalR, rate-limiting middleware) (RUNTIME)
- [ ] Verify that the `elsa/api` route prefix does not conflict with any existing API routes in representative host applications (CONFIG + RUNTIME)
- [ ] Confirm EF Core schema isolation works correctly when Elsa shares a database with the host application (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 3.2 Interoperability

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** Medium — integration points are visible in code; correctness of data exchange requires runtime validation

**What Claude assesses (code-level):**
- `Elsa.Http` provides bidirectional HTTP interoperability: inbound via `HttpWorkflowsMiddleware` (workflow-as-HTTP-endpoint) and outbound via `SendHttpRequest`-style activities.
- `Elsa.Workflows.Api` exposes a REST API surface with FastEndpoints across endpoints for definitions, instances, activity descriptors, bookmarks, tasks, events, and scripting — covering the full management plane.
- Swagger/OpenAPI integration is present via `Elsa.Api.Common` (`SwaggerExtensions.cs`) and FastEndpoints-based OpenAPI generation, enabling machine-readable API contracts.
- SAS token interoperability uses ASP.NET Core `IDataProtection` for time-limited, HMAC-protected tokens — a standard .NET interoperability mechanism.
- Scripting interoperability covers C#, JavaScript (Jint), Python (`Elsa.Expressions.Python`), and Liquid — enabling diverse host application scripting ecosystems.
- The `Elsa.Api.Client` project provides a typed .NET client library for REST API consumers, reducing integration friction.
- Assumption: message queue integration (RabbitMQ, Azure Service Bus etc.) is not present in this repository at this ref — the README mentions it but no corresponding module was found. This may be in a separate repository.

**Human completion required:**
- [ ] Confirm whether message bus integration modules (RabbitMQ, Azure Service Bus, MassTransit) are shipped as separate NuGet packages not in this repository (DESIGN)
- [ ] Validate OpenAPI spec accuracy against actual endpoint behaviour using automated contract tests (RUNTIME)
- [ ] Test `Elsa.Api.Client` against the server API with version-matched packages (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 4. Interaction capability

> Note: Elsa Core is a workflow engine library, not a user-facing UI product. All sub-characteristics below are assessed against the **developer/API consumer** experience. "Users" means developers embedding or consuming the library.

### 4.1 Appropriateness recognisability

**Evidence sources:** `CODE` `DOCS`
**Claude confidence:** Medium — entry points and feature names are inspected; discoverability experience is partly subjective

**What Claude assesses (code-level):**
- The primary entry point is `services.AddElsa(elsa => { ... })` — a single fluent registration method consistent with ASP.NET Core conventions. This matches what developers expect from a .NET library.
- The `UseXxx` pattern (`UseElsaScriptBlobStorage`, `UseWorkflowRuntime`, etc.) follows the established ASP.NET Core `IApplicationBuilder.Use*` idiom, aiding recognisability for .NET developers.
- 175 `*Feature*.cs` files expose discoverable capability toggles — each with `[ShellFeature(DisplayName = "...", Description = "...", DependsOn = [...])]` attributes that self-describe purpose and dependencies.
- The README is well-structured with a Docker quick-start, feature list, code examples (HTTP + email workflow in ~15 lines), and visual designer screenshots — reducing the time to first impression.
- NuGet documentation XML (`GenerateDocumentationFile=true` globally) is enabled — IntelliSense summaries are generated for all public APIs.

**Human completion required:**
- [ ] Survey new adopters on how long it takes to run the first workflow — time-to-hello-world is a key recognisability metric (OPS)
- [ ] Check whether the NuGet package descriptions and tags match the library's actual capabilities (CONFIG)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.2 Learnability

**Evidence sources:** `DOCS`
**Claude confidence:** Medium — README and inline documentation quality are assessed; external docs site content is not accessible from code

**What Claude assesses (code-level):**
- The README provides a quick-start Docker command, a C# workflow code example, and a visual designer screenshot — multiple learning paths are represented.
- The `doc/` directory exists at the repository root — its contents were not fully explored, but its presence suggests additional documentation artefacts.
- The `Elsa.Identity` module includes a `README.md` — suggesting module-level documentation practice exists, though coverage across all 50 modules is unverified.
- `src/common/Elsa.Testing.Shared/` and `Elsa.Testing.Shared.Integration/` provide helper classes (`WorkflowTestFixture`, `ActivityTestFixture`, `RunWorkflowResultAssertions`) that reduce the learning curve for adopters writing tests.
- Links to a dedicated documentation site (`v3.elsaworkflows.io` referenced indirectly) and Discord, Stack Overflow, and Gurubase AI are present in the README badges — multiple learning support channels exist.
- Assumption: depth and accuracy of the external docs site are not assessable from code alone.

**Human completion required:**
- [ ] Review the `doc/` directory contents and confirm documentation accuracy against the v3.7.0 API (DOCS)
- [ ] Verify that module-level README files exist for the most commonly used modules beyond `Elsa.Identity` (DOCS)
- [ ] Assess external documentation site (v3.elsaworkflows.io) completeness and currency against this release (DOCS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.3 Operability

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** Medium — management API surface and operational endpoints are inspectable

**What Claude assesses (code-level):**
- `/health/ready` and `/health/live` endpoints are registered (`services.AddHealthChecks()`, `app.MapHealthChecks("/")`), enabling readiness and liveness probing — a baseline operational requirement.
- The REST management API (`Elsa.Workflows.Api`) provides endpoints for workflow definitions (CRUD, publish/retract/revert), instances, activity executions, bookmarks, tasks, events, and features — covering the full operational lifecycle from outside the process.
- `InstanceHeartbeatService` / `InstanceHeartbeatMonitorService` provide node-level liveness tracking with configurable intervals — enabling distributed operations.
- The appsettings structure uses `reloadConfigOnChange: true` — configuration can be updated without restart for supported settings.
- Log levels are configurable per namespace in `appsettings.json` — operators can tune verbosity without code changes.
- Assumption: the health check at `/` (root path) is a simplification for the reference app; production deployments may need dedicated `/health/ready` and `/health/live` path mappings.

**Human completion required:**
- [ ] Confirm that health checks are wired to meaningful infrastructure probes (DB connectivity, message bus) rather than always returning healthy (CODE + RUNTIME)
- [ ] Verify that the management REST API supports pagination and filtering for large workflow instance datasets (CODE)
- [ ] Test the node heartbeat mechanism under simulated node failure to confirm detection latency meets operational SLAs (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.4 User error protection

**Evidence sources:** `CODE`
**Claude confidence:** High — validation patterns are directly inspectable in code

**What Claude assesses (code-level):**
- `ValidatingWorkflowDispatcher` validates channel existence before dispatch and returns a typed `DispatchWorkflowResponse.UnknownChannel()` error — preventing silent misdispatch.
- `DefaultAccessTokenIssuer` throws descriptive exceptions for missing signing key, issuer, or audience configuration (`"No signing key configured"`, `"No issuer configured"`, `"No audience configured"`) — failing fast at configuration time rather than silently issuing malformed tokens.
- Nullable reference types are enabled globally — the compiler enforces null-safety across all 50 projects, reducing null-dereference errors at runtime.
- `IWorkflowDefinitionPublisher` uses typed result objects (`PublishWorkflowDefinitionResult`) rather than raw booleans, giving callers structured error information.
- `IIncidentStrategy` provides a pluggable mechanism to handle activity faults — preventing unhandled exceptions from silently terminating workflows without record.
- `IWorkflowActivationStrategy` allows per-workflow control over duplicate-instance protection (e.g., singleton activation), preventing accidental duplicate workflow starts.
- Assumption: input validation on API endpoint request models is delegated to FastEndpoints' built-in validation — this was not directly examined.

**Human completion required:**
- [ ] Verify that API endpoints validate request models (required fields, format constraints) and return structured 400 Bad Request responses (CODE + RUNTIME)
- [ ] Confirm that `IIncidentStrategy` defaults (when no custom strategy is configured) surface faults visibly rather than swallowing them (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.5 User engagement

**Evidence sources:** `DESIGN` `RUNTIME`
**Claude confidence:** Very low — as a developer library, "engagement" maps to developer experience satisfaction, which cannot be assessed from code

**What Claude assesses (code-level):**
- The fluent builder API (`IWorkflowBuilder`, `WorkflowBase`), `AddElsa(elsa => ...)` registration, and `UseXxx` extension methods suggest deliberate attention to developer ergonomics.
- The visual designer integration (`Elsa Studio`) is referenced in README but lives in a separate repository — code-level evidence of the embedded designer experience is not present in this repository.
- The Docker quick-start with a single `docker run` command lowers the activation barrier, which correlates positively with developer engagement.
- Community channels (Discord, Stack Overflow) and AI assistant (Gurubase) are listed — indicating investment in the developer community experience.

**Human completion required:**
- [ ] Collect developer satisfaction metrics (GitHub stars trajectory, Discord activity, NuGet downloads) to assess engagement trends (OPS)
- [ ] Gather adopter feedback on the API ergonomics, particularly around workflow definition authoring in code vs. designer (OPS)
- [ ] Assess time-to-first-working-integration for a new developer unfamiliar with the library (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.6 Inclusivity

**Evidence sources:** `CODE`
**Claude confidence:** Medium — i18n and cultural sensitivity in a developer library is a narrow concern; assessed at log message and error message level

**What Claude assesses (code-level):**
- No `IStringLocalizer`, `.resx` resource files, or `ResourceManager` usage was found in any module — error messages and log strings are hardcoded in English. For a developer-facing library this is common and typically acceptable.
- `StringComparison.OrdinalIgnoreCase` and `InvariantCulture` comparisons appear in 43 files — indicating culture-safe string handling in identifiers and route comparison, which prevents locale-dependent bugs.
- No accessibility-specific code was found, which is expected for an API library without a UI component.
- The Docker entrypoint and setup scripts are in English. The README is English-only. Assumption: documentation in other languages is not provided.

**Human completion required:**
- [ ] Determine whether internationalisation of error messages is a requirement for target customer segments — if multi-language deployments are needed, localisation infrastructure is absent (DESIGN)
- [ ] Confirm that all route and identifier comparisons use `OrdinalIgnoreCase` consistently rather than relying on system locale (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.7 User assistance

**Evidence sources:** `DOCS`
**Claude confidence:** Medium — inline documentation and README quality are assessable; external docs site is not

**What Claude assesses (code-level):**
- `GenerateDocumentationFile=true` is set globally in `Directory.Build.props` — all public APIs emit XML documentation consumed by IDEs as IntelliSense tooltips.
- 59 out of 76 contracts in `Elsa.Workflows.Core/Contracts/` have `<summary>` tags — coverage is high but not complete. The suppressed warning `CS1591` (missing XML doc) means undocumented public members do not break the build.
- The `[ShellFeature(DisplayName = "...", Description = "...")]` attribute pattern embeds user-visible descriptions for every feature toggle — these descriptions surface in the feature discovery API (`/elsa/api/features`).
- Discord, Stack Overflow, and a linked docs site provide human-assisted support channels.
- The `IActivityDescriber` and `IActivityDescriptorModifier` interfaces suggest runtime activity self-description — activity metadata (name, description, ports) is available to tooling.
- Warning `CS1591` is suppressed globally — this reduces XML doc coverage enforcement.

**Human completion required:**
- [ ] Audit XML doc coverage across the 50 modules beyond the `Workflows.Core` contracts — the suppressed CS1591 warning means gaps may be widespread (CODE)
- [ ] Verify that the external documentation site (v3.elsaworkflows.io) references accurate API for v3.7.0 (DOCS)
- [ ] Assess whether activity descriptions in the designer and API surface are sufficient for non-author adopters (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 4.8 Self-descriptiveness

**Evidence sources:** `CODE`
**Claude confidence:** Medium — runtime self-description APIs are inspectable; richness of metadata is partly a runtime concern

**What Claude assesses (code-level):**
- `IActivityRegistry` and `IActivityDescriber` provide runtime enumeration and description of all registered activities — enabling tooling (designer, API clients) to discover capabilities without documentation.
- The `/elsa/api/features` endpoint (inferred from `Elsa.Workflows.Api/Endpoints/Features/`) exposes enabled feature state at runtime.
- `[ShellFeature(DisplayName, Description, DependsOn)]` attribute metadata is machine-readable — the module system can report its own configuration.
- `IActivity.Version` (integer) on each activity type provides version self-identification — important for workflow definition compatibility checks.
- The workflow JSON format carries type names and versions inline (`IActivity.Type`, `IActivity.Version`), making serialised workflow definitions self-describing.
- `IActivityDescriptorModifier` allows enrichment of activity descriptors post-registration — supporting dynamic self-description extensions.

**Human completion required:**
- [ ] Verify that the `/elsa/api/features` endpoint returns accurate feature state reflecting the actual DI registrations (RUNTIME)
- [ ] Confirm that the OpenAPI spec at the `swagger` endpoint accurately reflects all registered FastEndpoints routes (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 5. Reliability

### 5.1 Faultlessness

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Low — static analysis shows defensive patterns; actual defect rate requires production data

**What Claude assesses (code-level):**
- Exception handling appears in 267 source files — try/catch blocks are widespread. The `ResilientActivityInvoker` uses a `finally` block to ensure Polly context is always returned to the pool regardless of outcome.
- `DataProtectorTokenService.TryDecryptToken` catches all exceptions silently (bare `catch { }`) and returns `false` — this is intentional for a try-parse pattern but suppresses diagnostic information.
- Nullable reference types enabled globally reduce NullReferenceException risks.
- `ObsoleteAttribute` is used with `error: false` (soft deprecation) rather than hard errors — reducing the risk of compile-time breakage from API evolution while still guiding consumers.
- 12 unit + 9 integration test projects with 532 test methods provide regression protection, but coverage percentage is unknown.
- `TreatWarningsAsErrors=false` globally means code quality warnings do not block builds — a potential source of accumulated defects.

**Human completion required:**
- [ ] Obtain production defect rate and mean time between failures from an operational deployment (OPS)
- [ ] Enable and review compiler warning output to assess the volume of suppressed warnings (CODE)
- [ ] Run static analysis tools (Roslyn analyzers, SonarQube) and triage findings (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 5.2 Availability

**Evidence sources:** `CODE` `RUNTIME` `OPS`
**Claude confidence:** Low — availability is an operational measurement; code shows structural enablers only

**What Claude assesses (code-level):**
- Liveness and readiness health endpoints are registered (`app.MapHealthChecks("/")`) — infrastructure probes can detect unhealthy instances.
- `InstanceHeartbeatService` writes periodic heartbeats to a key-value store; `InstanceHeartbeatMonitorService` reads and evaluates them — providing cluster-level node availability awareness.
- Distributed locking via Medallion.Threading ensures workflow execution is coordinated across nodes without requiring a single-instance deployment.
- The `BookmarkQueueWorker` pattern decouples workflow execution from trigger delivery — message queue durability (if an external broker is used) can extend effective availability.
- Health checks at `app.MapHealthChecks("/")` are on the root path in the reference app, which may conflict with normal application responses. Production deployments should map these to dedicated paths.

**Human completion required:**
- [ ] Measure uptime SLA in a representative deployment over at least 30 days (OPS)
- [ ] Confirm health check probes are connected to actual dependency health (DB, message bus) and not trivially returning 200 (RUNTIME)
- [ ] Test rolling deployment (node drain and restart) to verify zero-downtime upgrade behaviour (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 5.3 Fault tolerance

**Evidence sources:** `CODE`
**Claude confidence:** Medium — Polly integration, incident strategies, and distributed lock usage are directly inspectable

**What Claude assesses (code-level):**
- `Elsa.Resilience.Core` integrates Polly v8 (`Polly`, `Polly.Extensions` package references) via `IResilienceStrategy` and `ResilientActivityInvoker`. Activities implementing `IResilientActivity` can declare a resilience strategy (retry, circuit-breaker, hedging) per-activity through configurable strategy catalogs.
- `IIncidentStrategy` / `IIncidentStrategyResolver` provide pluggable fault handling at the workflow level — when an activity faults, the strategy determines whether to suspend, fault the workflow, or retry.
- `ITransientExceptionDetector` / `DefaultTransientExceptionStrategy` distinguish transient from permanent failures — Polly retries are applied only to transient exceptions.
- `RetryTelemetryListener` emits retry attempt telemetry through Polly's telemetry pipeline — attempts are recorded and surfaced in the designer via `context.SetRetriesAttemptedFlag()`.
- `IResilienceStrategyCatalog` allows per-deployment strategy registration — operators can define environment-specific retry policies without code changes.
- Distributed locking (`Medallion.Threading`) prevents concurrent execution of the same workflow instance across nodes — a fault isolation boundary.

**Human completion required:**
- [ ] Test fault tolerance under simulated transient failure (DB unavailability, network timeouts) to verify retry policies engage correctly (RUNTIME)
- [ ] Verify that the circuit-breaker strategy (if provided as a built-in strategy) opens under sustained failure and closes on recovery (RUNTIME)
- [ ] Confirm that unhandled activity exceptions do not silently terminate the workflow process without state persistence (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 5.4 Recoverability

**Evidence sources:** `CODE` `OPS`
**Claude confidence:** Low — state persistence architecture is clear from code; actual recovery time requires failure injection testing

**What Claude assesses (code-level):**
- `IWorkflowStateSerializer` (with `JsonWorkflowStateSerializer` implementation) serialises complete workflow execution state — this state is persisted to `IWorkflowInstanceStore`, enabling resume from any persisted checkpoint.
- The bookmark system (`IBookmarkStore`, `IBookmarkPersister`) persists suspension points, allowing long-running workflows to resume after process restart.
- `IWorkflowDefinitionStore` / `IWorkflowInstanceStore` abstractions support EF Core providers for all major RDBMS — state persistence durability depends on the provider and its transaction guarantees.
- EF Core migrations are applied via `RunMigrationsHostedService` on startup — schema recovery is automated.
- The distributed runtime's `DistributedBookmarkQueueWorker` handles bookmark processing with distributed lock protection — preventing double-processing after node recovery.
- Assumption: whether workflow instances interrupted mid-execution (process crash between activity completions) can be correctly resumed depends on the persistence provider's transaction boundary — this requires runtime validation.

**Human completion required:**
- [ ] Test crash recovery: kill the process mid-workflow-execution and verify the workflow resumes correctly after restart (RUNTIME)
- [ ] Measure recovery time objective (RTO) under a process restart scenario with the default EF Core Sqlite provider (RUNTIME)
- [ ] Verify that partially-executed activity state (activity started but not completed) is handled correctly on resume (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 6. Security

### 6.1 Confidentiality

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** High — security configurations and data protection patterns are directly inspectable

**What Claude assesses (code-level):**
- SAS tokens use `Microsoft.AspNetCore.DataProtection.IDataProtector` (AES-256 GCM by default in ASP.NET Core) for time-limited token encryption — `DataProtectorTokenService` wraps payload serialization and protection/unprotection.
- JWT tokens carry per-user `TenantId` claims — tenant context is embedded in the access token and cannot be spoofed without the signing key.
- API keys are hashed (SHA-256 + random salt via `DefaultSecretHasher`) before storage — plaintext secrets are not persisted.
- The `appsettings.json` reference configuration includes a sample `SigningKey: "sufficiently-large-secret-signing-key"` — this is a placeholder; production must override. No hardcoded production secrets were found in source, but the sample config uses a weak key name that could be copy-paste deployed.
- Multi-tenant EF Core query filters (`SetTenantIdFilter`) apply `HasQueryFilter` globally via `IEntityModelCreatingHandler` — tenant-scoped queries enforce data confidentiality at the ORM layer.
- No TLS configuration is enforced in code (left to ASP.NET Core hosting configuration) — this is appropriate for a library but must be documented for operators.

**Human completion required:**
- [ ] Verify that production appsettings do not contain `"sufficiently-large-secret-signing-key"` or other placeholder values (CONFIG)
- [ ] Confirm that the `DataProtection` key ring is configured with a durable key store (Azure Key Vault, filesystem, etc.) and not the ephemeral in-process default in production (CONFIG)
- [ ] Audit that tenant ID query filters are correctly applied to all entity types in all EF Core configurations (not only `Elsa.Persistence.EFCore.Common`) (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 6.2 Integrity

**Evidence sources:** `CODE`
**Claude confidence:** High — input validation, SAS token integrity, and workflow state integrity patterns are inspectable

**What Claude assesses (code-level):**
- SAS tokens use `IDataProtector.ToTimeLimitedDataProtector().Protect()` — any tampered or expired token fails decryption and is rejected at the `TryDecryptToken` level before any workflow action is taken.
- JWT signature validation is enforced via `ConfigureJwtBearerOptions` and `ValidateIdentityTokenOptions` — forged tokens are rejected.
- Workflow definitions carry `int Version` on each activity — version mismatch detection supports integrity of workflow execution against stale definitions.
- EF Core `SaveChangesAsync` runs pre-save handlers (`IEntitySavingHandler`) — providing a hook for integrity enforcement (e.g., `ApplyTenantId` sets tenant ownership on creation).
- `IWorkflowStateSerializer` uses `System.Text.Json` — JSON serialisation with type discriminators, not binary/custom formats — reducing integrity risks from format confusion.
- `ValidatingWorkflowDispatcher` validates channel integrity before dispatch — unconfigured channels are rejected.

**Human completion required:**
- [ ] Verify that workflow definition imports (JSON import via API) validate schema and activity type compatibility before persisting (CODE + RUNTIME)
- [ ] Confirm that the `IEntitySavingHandler` pipeline cannot be bypassed by direct EF Core operations in custom code (DESIGN)
- [ ] Test that tampered SAS tokens are rejected with appropriate HTTP error responses (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 6.3 Non-repudiation

**Evidence sources:** `CODE`
**Claude confidence:** Medium — execution log storage exists; completeness of audit trail requires deeper review

**What Claude assesses (code-level):**
- `IActivityExecutionStore` (in `Elsa.Workflows.Runtime/Contracts/`) and `StoreActivityExecutionLogSink` / `StoreWorkflowExecutionLogSink` persist per-activity execution records — these records provide an execution-level audit trail.
- `RetryAttemptRecord` persists retry attempt history including `ActivityInstanceId`, `WorkflowInstanceId`, `AttemptNumber`, and `RetryDelay` — retried operations are traceable.
- JWT claims include the user `Name` and `TenantId` — authentication events can be correlated with workflow actions if the JWT is propagated through execution contexts.
- `ILogRecordStore` / `ILogRecordSink` abstract log record persistence — these are distinct from the application log (ILogger) and represent workflow-level event records.
- No explicit audit event for authentication or API access was observed at the code level (e.g., no "user logged in", "workflow definition modified by X" event emitter). This is a potential gap for compliance use cases.

**Human completion required:**
- [ ] Determine whether regulatory or contractual requirements mandate full audit trails for workflow operations — if so, assess gap between current execution log and a full audit log (DESIGN)
- [ ] Verify that `IActivityExecutionStore` records include sufficient actor identity (user/app ID) to support non-repudiation claims (CODE)
- [ ] Confirm that log records are immutable once written (no update/delete API exposed) (CODE + RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 6.4 Accountability

**Evidence sources:** `CODE`
**Claude confidence:** High — identity model and token structure are directly inspectable

**What Claude assesses (code-level):**
- JWT tokens embed `Name`, `TenantId`, roles, and permissions claims — authenticated API requests carry caller identity in the token.
- The `DefaultAccessTokenIssuer` encodes `user.Name` as a `JwtRegisteredClaimNames.Name` claim — the acting user is cryptographically bound to each token.
- API key authentication (`DefaultApiKeyProvider`, `AdminApiKeyProvider`) associates each key with an `Application` entity, which has an `Id`, `Name`, and `TenantId` — application-level actions can be attributed to a named application.
- `ClaimsTenantResolver` and `CurrentUserTenantResolver` extract tenant context from claims — multi-tenant operations are tenant-attributed.
- `IUserManager` / `IRoleManager` manage user-role assignments — role-based access enables post-hoc attribution of permissions to actions.
- No code-level evidence of per-request access logging (HTTP access log emitting user identity per API call) was found — this may be delegated to ASP.NET Core middleware or infrastructure.

**Human completion required:**
- [ ] Verify that ASP.NET Core request logging or application-level middleware captures authenticated user identity per API request (RUNTIME + CONFIG)
- [ ] Confirm that `IActivityExecutionStore` records correlate to the authenticated user/app that triggered the workflow (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 6.5 Authenticity

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** High — authentication schemes and configuration are directly readable

**What Claude assesses (code-level):**
- `DefaultAuthenticationFeature` configures a multi-scheme policy (`Jwt-or-ApiKey`) that routes authentication based on the `Authorization` header format — JWT Bearer for token-based clients, API key for application clients.
- JWT Bearer validation is configured via `ConfigureJwtBearerOptions` and `ValidateIdentityTokenOptions` — token signature, issuer, audience, and expiry are all validated.
- `DefaultSecretHasher` uses SHA-256 with cryptographically random 32-byte salt (`RandomNumberGenerator.GetBytes(32)`) for password and secret hashing — password storage follows a salted-hash pattern. However, SHA-256 is a fast hash; for password hashing, a slow adaptive function (BCrypt, Argon2, PBKDF2) is the current security best practice. This is a notable finding.
- `IRandomStringGenerator` / `DefaultClientIdGenerator` / `DefaultSecretGenerator` use cryptographically random generation for client IDs and secrets.
- `LocalHostRequirementHandler` / `LocalHostPermissionRequirementHandler` allow localhost bypass — this must be disabled in production; its presence in the feature set is appropriate for development convenience.

**Human completion required:**
- [ ] Evaluate whether SHA-256 + salt is sufficient for the threat model, or whether a slow adaptive password hash (BCrypt/Argon2/PBKDF2) should replace it — this is a security finding that should be assessed by a security engineer (CODE)
- [ ] Confirm that `LocalHostRequirementHandler` bypass is disabled or restricted in production deployments (CONFIG)
- [ ] Test that expired and invalid-issuer JWT tokens are correctly rejected with 401 responses (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 6.6 Resistance

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Medium — structural resistance patterns are visible; exploit testing requires runtime assessment

**What Claude assesses (code-level):**
- `NuGetAudit=enable` / `NuGetAuditMode=all` in `Directory.Build.props` — all dependencies (including transitive) are audited for known CVEs at build time. This is a proactive supply-chain resistance measure.
- `EnableTrimAnalyzer=true` is set — IL trimming analysis is enabled, which can expose unsafe reflection patterns that could be exploited in trimmed deployments.
- No evidence of input sanitization or output encoding beyond JSON serialization was found — for HTTP trigger workflows that accept untrusted HTTP body content, this may be relevant.
- Rate limiting is present only in `RateLimitedFuncExtensions` (bookmark queue processing), not at the API ingress layer — no ASP.NET Core rate limiting middleware was observed in `Program.cs`.
- No CORS policy configuration was found in the reference `Program.cs` scan — CORS posture is unclear.
- The `DataProtectorTokenService` silently catches all decryption failures — this prevents error oracle attacks on SAS tokens.

**Human completion required:**
- [ ] Add API-level rate limiting (ASP.NET Core `RateLimiter` middleware) and confirm it is applied to sensitive endpoints (workflow trigger, authentication) (CODE + CONFIG)
- [ ] Perform penetration testing on the REST API surface, particularly HTTP workflow endpoints that accept untrusted external input (RUNTIME)
- [ ] Configure and validate CORS policy to restrict cross-origin access to the management API (CONFIG)
- [ ] Verify that NuGet audit results are clean (no known high/critical CVEs in dependencies) at this release (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 7. Maintainability

### 7.1 Modularity

**Evidence sources:** `CODE`
**Claude confidence:** Medium — module boundaries and dependency declarations are directly inspectable

**What Claude assesses (code-level):**
- The repository contains 50 distinct source projects in `src/`, each as a separate NuGet package with explicit project references — no circular references are enforced by the build system's project graph.
- 141 `IShellFeature`/`IFeature`-implementing files and 175 `*Feature*.cs` files declare `DependsOn` chains — feature-to-feature dependencies are explicit and machine-readable.
- The module system (`Elsa.Common/Elsa.Features`) provides a `Module` abstraction that aggregates features into cohesive units (`AddElsa(...)`) while keeping individual features independently installable.
- Core abstractions (`IActivity`, `IWorkflowRunner`, `IWorkflowRuntime`, store interfaces) live in `Elsa.Workflows.Core` and `Elsa.Workflows.Management` — implementations live in separate provider modules (`Elsa.Persistence.EFCore.*`). This pattern prevents implementation leakage into the core.
- The `Elsa.Resilience.Core` / `Elsa.Resilience` split follows the same core/implementation pattern.
- Assumption: circular dependencies between non-project namespaces within a single project were not checked — namespace-level coupling is not assessed here.

**Human completion required:**
- [ ] Run dependency analysis (e.g., `dotnet-depends`, NDepend) to visualise cross-project dependency graph and identify any unexpected coupling (CODE)
- [ ] Verify that adding a new persistence provider requires only implementing the store interfaces and registering a new feature — no changes to core modules (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 7.2 Reusability

**Evidence sources:** `CODE`
**Claude confidence:** Medium — abstraction layers and generic patterns are directly inspectable

**What Claude assesses (code-level):**
- 76 interfaces in `Elsa.Workflows.Core/Contracts/` alone define a broad, stable abstraction surface. Each interface is a seam for substitution and reuse.
- `Elsa.Testing.Shared` and `Elsa.Testing.Shared.Integration` provide reusable test infrastructure (`WorkflowTestFixture`, `ActivityTestFixture`, `RunWorkflowResultAssertions`) — consumers can reuse these without duplication.
- The `IResilienceStrategy` / `IResilienceStrategyCatalog` pattern allows custom resilience strategies to be registered and reused across activities.
- 87 files use typed `Input<T>` / `Output<T>` generics — the generic activity port pattern is reusable across all activity implementations.
- The scheduling (`IScheduler`, `IWorkflowScheduler`) and expressions (`IExpressionHandler`) abstractions are designed for substitution, with multiple implementations per interface.
- `ConfigureAwait.Fody` weaving is reusable infrastructure — new projects that add `Fody` and `ConfigureAwait.Fody` to their project file inherit the pattern without code changes.

**Human completion required:**
- [ ] Assess whether external consumers (NuGet users) can implement and register custom persistence providers, custom activities, and custom resilience strategies without forking the repository (CODE + DESIGN)
- [ ] Review whether testing shared infrastructure is published as a separate NuGet package for test project consumers (CONFIG)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 7.3 Analysability

**Evidence sources:** `CODE` `OPS`
**Claude confidence:** Medium — logging and diagnostic patterns are inspectable; production observability requires operational tooling review

**What Claude assesses (code-level):**
- `ILogger<T>` is used across 106 source files — structured logging is pervasive. Log levels are configurable per namespace in appsettings.
- `IActivityExecutionStore`, `ILogRecordStore`, and `StoreWorkflowExecutionLogSink` provide workflow-level execution records separate from the application log — enabling post-mortem analysis.
- `RetryTelemetryListener` emits retry attempt data through Polly telemetry — retry behaviour is observable.
- `ILoggerStateGenerator` (contract in `Workflows.Core`) enables custom log state enrichment per activity — diagnostic context can be injected.
- No OpenTelemetry (`ActivitySource`, `System.Diagnostics.Activity`) instrumentation was found in the core execution path — distributed tracing is not natively emitted. Assumption: OpenTelemetry may be configured externally via ASP.NET Core's built-in instrumentation for HTTP, but workflow-level spans are not emitted.
- The Datadog Docker compose file (`docker-compose-datadog+otel-collector.yml`) in `src/apps/Elsa.Server.Web/` suggests intent for observability integration, but no matching `ActivitySource` code was found.

**Human completion required:**
- [ ] Determine whether OpenTelemetry distributed tracing spans are emitted for workflow and activity execution — if not, consider adding `ActivitySource` instrumentation to the execution pipeline (CODE)
- [ ] Verify that the Datadog/OTel collector compose file wires up correctly with the application's logging/metrics output (CONFIG + RUNTIME)
- [ ] Confirm that workflow execution log records are queryable with sufficient filters to support incident investigation (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 7.4 Modifiability

**Evidence sources:** `CODE`
**Claude confidence:** Medium — extension points and deprecation patterns are inspectable

**What Claude assesses (code-level):**
- `IActivityExecutionPipelineBuilder` and `IWorkflowExecutionPipelineBuilder` use a middleware chain pattern — new cross-cutting behaviours can be inserted without modifying existing components.
- `IEntityModelCreatingHandler` and `IEntitySavingHandler` allow EF Core behaviour extension without subclassing `ElsaDbContextBase` — open/closed principle is applied.
- `IWorkflowActivationStrategy`, `IIncidentStrategy`, and `IResilienceStrategy` are all extension points — common customisation needs are covered by stable seams.
- `[Obsolete("...", error: false)]` is used (e.g., `IWorkflowDefinitionPublisher.New`) rather than hard removals — backward compatibility is maintained during migration periods. The comment in `Directory.Build.props` explicitly states: "Obsolete API warnings - Suppressed for backward compatibility during migration period".
- `CS0618` (obsolete API usage) is suppressed globally — this means internal obsolete API calls do not produce build warnings and may indicate areas that need cleanup.
- `TreatWarningsAsErrors=false` reduces code quality enforcement during modification.

**Human completion required:**
- [ ] Review the list of `[Obsolete]` members to determine if any have been pending removal for multiple release cycles — plan removal schedule to avoid obsolete API accumulation (CODE)
- [ ] Assess whether the middleware pipeline pattern is documented sufficiently for contributors to add new middleware components (DOCS)
- [ ] Consider enabling `TreatWarningsAsErrors` for a specific warning subset (e.g., CS8600–CS8629 nullable warnings) to enforce modifiability discipline (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 7.5 Testability

**Evidence sources:** `CODE`
**Claude confidence:** High — test infrastructure and interface-based design are directly inspectable

**What Claude assesses (code-level):**
- `IActivityTestRunner` and `IWorkflowRunner` interfaces allow test harnesses to run workflows in isolation via DI substitution.
- `Elsa.Testing.Shared` provides `FakeActivityExecutionContextSchedulerStrategy`, `FakeWorkflowExecutionContextSchedulerStrategy`, `XunitLogger`, and `CapturingTextWriter` — a dedicated fake/stub library exists for test support.
- `WorkflowTestFixture` and `ActivityTestFixture` provide base classes for integration and unit tests respectively — reducing per-test setup boilerplate.
- 12 unit test projects and 9 integration test projects exist — the testing effort is structurally separated by scope.
- 1 BenchmarkDotNet performance test project exists — performance regression testing is in place, albeit with a single benchmark.
- All major services implement interfaces — every service can be replaced with a test double at the DI layer.
- `IActivityTestRunner.RunAsync(WorkflowGraph, IActivity)` supports running individual activities in isolation — unit-level activity testing is supported by design.
- `RunWorkflowResultAssertions` provides assertion helpers — fluent assertion patterns reduce test verbosity.

**Human completion required:**
- [ ] Measure and report code coverage percentage for the unit and integration test suites (RUNTIME)
- [ ] Identify whether there are any testability gaps in the distributed runtime path (`DistributedWorkflowRuntime`) — distributed scenarios may require additional fake infrastructure (CODE)
- [ ] Expand the BenchmarkDotNet suite to cover multi-activity workflow patterns, not only `WriteLine` (CODE)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 8. Flexibility

### 8.1 Adaptability

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** Medium — configuration extension points and provider swap patterns are inspectable

**What Claude assesses (code-level):**
- All persistence, scheduling, identity, caching, and resilience concerns are abstracted behind interfaces and registered via the feature system — swapping any component (e.g., switching from EF Core SQLite to PostgreSQL) requires only a feature configuration change.
- `appsettings.json` binding via `IOptions<T>` with `ShellConfiguration` sections allows all configurable values to be overridden without code changes — runtime configuration adaptation is supported.
- Multi-tenant configuration (`Multitenancy.Tenants[]`) allows per-tenant connection strings, HTTP prefixes, and other settings to be independently configured.
- `IStorageDriver` and `IBlobWorkflowFormatHandler` abstractions allow storage backends to be adapted without core changes.
- Expression language is pluggable — C#, JavaScript, Python, and Liquid are independently replaceable or addable by implementing `IExpressionHandler`.
- `HostBuilder.reloadConfigOnChange: true` enables live configuration reload without restart.
- EF Core schema name and migrations history table are statically configurable properties (`ElsaDbContextBase.ElsaSchema`, `MigrationsHistoryTable`) — schema adaptation for shared database deployments is supported.

**Human completion required:**
- [ ] Verify that swapping a persistence provider (e.g., EF Core SQLite to PostgreSQL) in a running deployment requires only configuration changes and migration application (RUNTIME)
- [ ] Confirm that custom expression language providers can be registered externally without modifying this repository (CODE)
- [ ] Test multi-tenant configuration reload to confirm tenant settings changes are picked up without restart (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 8.2 Scalability

**Evidence sources:** `CODE` `CONFIG` `RUNTIME`
**Claude confidence:** Low — distributed architecture patterns are visible; actual scaling behaviour requires load testing

**What Claude assesses (code-level):**
- `Elsa.Workflows.Runtime.Distributed` implements `IWorkflowRuntime` with distributed locking, distributed definition refresh/reload, and distributed bookmark queue workers — horizontal scaling of the workflow runtime is architecturally supported.
- Medallion.Threading is used for distributed locking — the lock provider is abstracted, supporting Redis, SQL Server, PostgreSQL, and file-system backends for lock coordination.
- `DistributedBookmarkQueueWorker` and `DistributedWorkflowDefinitionsReloader` coordinate across nodes using distributed locks and signals — preventing work duplication under multi-node deployments.
- `InstanceHeartbeatService` / `InstanceHeartbeatMonitorService` provide node-level registration and monitoring — a prerequisite for cluster-aware routing.
- The background dispatch pattern (`BackgroundWorkflowDispatcher`, `BackgroundStimulusDispatcher`) decouples trigger receipt from execution — enabling queue-based horizontal scaling with an external message broker.
- Docker Compose files (`docker-compose-kafka.yml`) suggest Kafka integration has been tested — indicating message-bus-backed scalability patterns have been explored.

**Human completion required:**
- [ ] Conduct horizontal scale-out test: run 3+ nodes with a shared PostgreSQL database and distributed Redis lock provider; verify no duplicate executions or lost workflows (RUNTIME)
- [ ] Define and document maximum supported workflow instance concurrency per node configuration (OPS)
- [ ] Verify that `Medallion.Threading` lock provider selection (Redis/SQL/PostgreSQL) is documented for production deployments (CONFIG)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 8.3 Installability

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** High — Docker, NuGet, and CI/CD artefacts are directly inspectable

**What Claude assesses (code-level):**
- Four Dockerfiles are present (`ElsaServer.Dockerfile`, `ElsaServer-Datadog.Dockerfile`, `ElsaServerAndStudio.Dockerfile`, `ElsaStudio.Dockerfile`) — multiple Docker image variants are maintained for different deployment configurations.
- `docker-compose.yml`, `docker-compose-kafka.yml`, and `docker-compose-datadog+otel-collector.yml` provide ready-to-use orchestration configurations for standard and advanced scenarios.
- `entrypoint.sh` and `init-db-postgres.sh` scripts handle startup initialisation and PostgreSQL database setup — reducing manual configuration steps.
- EF Core migrations are applied automatically at startup via `RunMigrationsHostedService` — zero-configuration database installation.
- The `packages.yml` GitHub Actions workflow publishes NuGet packages — the NuGet distribution channel is CI-managed.
- The `pr.yml` CI pipeline runs `Compile + Test` on Ubuntu with .NET 10 on every PR — installability regressions would be caught early.
- A Docker quick-start (`docker pull` + `docker run`) is documented in the README with a single command — installation friction is minimal for evaluation.
- `setup/` directory in `Elsa.Server.Web` exists — contents were not inspected but suggest additional setup documentation.

**Human completion required:**
- [ ] Verify that the Docker images are published to Docker Hub and tagged correctly for v3.7.0 (CONFIG)
- [ ] Test fresh installation from NuGet on a new ASP.NET Core project to confirm the getting-started steps in the README are accurate for this release (RUNTIME)
- [ ] Confirm that `RunMigrationsHostedService` handles migration conflicts gracefully in multi-node deployments (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 8.4 Replaceability

**Evidence sources:** `CODE` `DESIGN`
**Claude confidence:** Low — replaceability requires knowledge of actual adopter migration patterns, which are operational

**What Claude assesses (code-level):**
- The persistence abstraction (`IWorkflowDefinitionStore`, `IWorkflowInstanceStore`, `IBookmarkStore`, etc.) decouples Elsa from any specific persistence technology — a host application can replace the entire persistence layer by registering alternative implementations.
- The `IWorkflowRuntime` interface means the runtime implementation (`LocalWorkflowClient`, `DistributedWorkflowRuntime`) is replaceable — an alternative runtime (e.g., Azure Durable Functions bridge) could be substituted.
- Workflow state is serialized to JSON (`IWorkflowStateSerializer`) — the persisted state is a documented, version-stamped format, making it portable if migrating to a different runtime.
- `[Obsolete]` markers (with `error: false`) document migration paths for adopters replacing deprecated APIs — a replaceability aid for version-to-version migration.
- Assumption: whether Elsa can be gradually replaced by a different workflow engine in an existing deployment (partial migration) depends on host application integration patterns and is not assessable from code.

**Human completion required:**
- [ ] Document a migration guide for moving from Elsa 2.x to 3.x and from earlier 3.x versions to 3.7.0 (DOCS)
- [ ] Assess whether workflow state stored in earlier schema versions can be migrated to v3.7.0 without data loss (RUNTIME)
- [ ] Gather feedback from adopters who have swapped persistence providers or upgraded from earlier versions (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## 9. Safety

> Note: Elsa is a workflow engine library. Physical safety does not apply. Safety sub-characteristics below are interpreted in the context of: preventing data loss, preventing financial harm from incorrect workflow execution, preventing system corruption, and preventing uncontrolled external action from misconfigured workflows.

### 9.1 Operational constraint

**Evidence sources:** `CODE` `DESIGN`
**Claude confidence:** Low — operational constraints depend on deployment policies outside the library's scope

**What Claude assesses (code-level):**
- `IWorkflowActivationStrategy` allows deployers to constrain workflow instantiation (singleton, correlation-scoped) — preventing uncontrolled parallel execution of dangerous workflows.
- `ValidatingWorkflowDispatcher` enforces channel constraints before dispatch — unconfigured channels are rejected rather than silently ignored.
- The `FaultBehaviour` mechanism (via `IIncidentStrategy`) allows workflows to be halted on specific error conditions — providing an operational constraint boundary.
- `CancellationToken` propagation is pervasive (730 source files) — external cancellation can stop workflow execution mid-flight.
- No built-in rate limiting on workflow trigger endpoints was found — an unconstrained public HTTP trigger endpoint could be abused to generate unbounded workflow instances.

**Human completion required:**
- [ ] Define and implement rate limits on public-facing HTTP workflow trigger endpoints (`/workflows/...`) (CODE + CONFIG)
- [ ] Document recommended `IWorkflowActivationStrategy` settings for workflows that invoke external financial or transactional systems (DESIGN)
- [ ] Confirm that workflow cancellation propagates correctly through all async paths, including external HTTP calls in activities (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 9.2 Risk identification

**Evidence sources:** `DESIGN` `OPS`
**Claude confidence:** Very low — risk identification is a design and operational concern; not assessable from code alone

**What Claude assesses (code-level):**
- No built-in risk identification or workflow safety analysis tooling was found in the codebase — risk identification is entirely the responsibility of the adopter and their workflow design.
- The `Elsa.Alterations` module allows in-flight workflow modification — this introduces a risk of unintended state transitions if alteration plans are incorrectly specified. No guardrails or simulation mode for alteration plans were observed.
- The `IIncidentStrategy` system surfaces workflow faults — but does not proactively predict or warn about risky workflow patterns at definition time.
- Assumption: the library does not claim to provide workflow safety analysis; this is a domain-level concern for adopters.

**Human completion required:**
- [ ] Conduct a domain-level risk assessment for any workflows that invoke financial transactions, external APIs with side effects, or irreversible operations (DESIGN)
- [ ] Assess whether the `Elsa.Alterations` alteration plan mechanism requires additional validation or simulation capability before applying in production (DESIGN)
- [ ] Document known risk scenarios and recommended mitigations in the operational documentation (DOCS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 9.3 Fail safe

**Evidence sources:** `CODE` `RUNTIME`
**Claude confidence:** Low — fail-safe patterns in the code are observable; effectiveness under failure requires injection testing

**What Claude assesses (code-level):**
- When an activity faults, `IIncidentStrategy` determines the response — the pluggable strategy means fail-safe behaviour is configurable per deployment. The built-in strategies can halt the workflow (preventing further action) or retry.
- Polly pipeline integration (`ResilientActivityInvoker`) ensures that transient activity failures do not immediately corrupt workflow state — retries and circuit-breakers provide buffering.
- Distributed locking ensures that a workflow instance is executed on at most one node at a time — preventing split-brain execution that could cause double-actions.
- `WorkflowState` is persisted before suspension points (bookmarks) — a crash during execution loses at most the current activity's progress, not prior completed steps.
- The `DataProtectorTokenService.TryDecryptToken` silently fails on tampered tokens rather than throwing — token-based features fail closed (deny access) rather than open.
- `WorkflowHost.RunWorkflowAsync` logs a warning and returns without executing when the workflow is not in the `Running` state — preventing unintended action on terminated workflows.

**Human completion required:**
- [ ] Inject database failure mid-execution and verify workflow state is recoverable from the last persisted bookmark (RUNTIME)
- [ ] Test what happens when the distributed lock provider is unavailable — confirm the system fails safely rather than proceeding without locking (RUNTIME)
- [ ] Document the default `IIncidentStrategy` configuration and its fail-safe behaviour for adopters (DOCS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 9.4 Hazard warning

**Evidence sources:** `CODE`
**Claude confidence:** Medium — warning patterns in code are directly inspectable

**What Claude assesses (code-level):**
- `WorkflowHost.RunWorkflowAsync` emits `LogWarning` when a resume is attempted on a non-Running workflow — a code-level hazard warning to operators.
- `DefaultAccessTokenIssuer` throws descriptive exceptions for missing security configuration (`"No signing key configured"`) — a startup-time hazard warning.
- The `RetryTelemetryListener` records retry attempt metadata — operators can observe retry storms as a leading indicator of service degradation.
- `InstanceHeartbeatMonitorService` raises a notification (`INotificationSender`) when node heartbeats are missed — a cluster-level hazard warning mechanism.
- `ValidatingWorkflowDispatcher` returns a typed `DispatchWorkflowResponse.UnknownChannel()` — callers receive an explicit signal rather than a silent no-op.
- The reference `appsettings.json` includes a comment-like placeholder signing key (`"sufficiently-large-secret-signing-key"`) — this is a hazard if deployed to production without replacement. No runtime warning is emitted for weak signing keys.

**Human completion required:**
- [ ] Add a startup warning or validation check that emits a log warning (at minimum) when the signing key matches the default placeholder value (CODE)
- [ ] Verify that heartbeat miss notifications are observable through a configured alerting channel in production (CONFIG + OPS)
- [ ] Confirm that retry storm patterns (high retry counts within a short window) are surfaced through monitoring dashboards (OPS)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

### 9.5 Safe integration

**Evidence sources:** `CODE` `CONFIG`
**Claude confidence:** Low — integration safety depends on host application configuration and external system behaviour, both outside this codebase

**What Claude assesses (code-level):**
- `CancellationToken` is accepted on all external-facing activity execution methods — integration calls can be cancelled without orphaning workflow state.
- SAS tokens for public event/bookmark triggers are time-limited (`ToTimeLimitedDataProtector()`) — integration endpoints have built-in expiry to reduce exposure window.
- `DataProtectorTokenService` wraps SAS token operations with ASP.NET Core Data Protection — integration secrets are encrypted in transit.
- HTTP workflow triggers authenticate the caller before executing workflows (through the JWT/API key middleware) — external HTTP integrations require credentials.
- The `Elsa.Http` module's `HttpWorkflowsMiddleware` routes inbound HTTP to workflow bookmarks/triggers — the mapping is based on a SHA-256 bookmark hash, which provides a non-enumerable trigger namespace.
- No input schema validation was observed for inbound HTTP workflow trigger payloads — malformed or oversized payloads may reach activity code without prior validation.

**Human completion required:**
- [ ] Implement request size limits and basic schema validation on HTTP workflow trigger endpoints to prevent resource exhaustion (CODE + CONFIG)
- [ ] Audit all activities that make outbound HTTP calls (e.g., `SendHttpRequest`) for timeout configuration and redirected-request safety (CODE)
- [ ] Test that cancelling a workflow execution mid-HTTP-call correctly cleans up the outbound HTTP request (RUNTIME)

**Verdict (human to complete):**
[ ] ✅ Meets requirements  [ ] 🟡 Partially meets  [ ] ❌ Does not meet  [ ] ⬜ Cannot assess
Notes: ___
Reviewed by: ___ · Date: ___

---

## Summary and sign-off

### Verdict summary
*Complete this table after all individual verdicts are filled in.*

| Characteristic | Sub-characteristics | ✅ Meets | 🟡 Partial | ❌ Does not meet | ⬜ Cannot assess |
|---|---|---|---|---|---|
| Functional suitability | 3 | | | | |
| Performance efficiency | 3 | | | | |
| Compatibility | 2 | | | | |
| Interaction capability | 8 | | | | |
| Reliability | 4 | | | | |
| Security | 6 | | | | |
| Maintainability | 5 | | | | |
| Flexibility | 4 | | | | |
| Safety | 5 | | | | |
| **Total** | **40** | | | | |

---

### Top findings from Claude's code analysis
*Three strongest positive signals, three most significant gaps.*

**Strongest evidenced qualities:**

1. **Deep security layering with good separation of concerns.** Multi-scheme authentication (JWT Bearer + API key), HMAC-signed SAS tokens via ASP.NET Core Data Protection, per-entity tenant isolation via EF Core global query filters (`SetTenantIdFilter`), and salted password hashing are all present and structurally sound. The `NuGetAudit=all` build-time supply-chain check adds a proactive defence layer.

2. **Exceptionally high testability by design.** Every significant service is behind an interface; a dedicated `Elsa.Testing.Shared` library provides fakes, fixtures, and assertion helpers; and 12 unit + 9 integration test projects with 532 test methods demonstrate sustained investment in testability. The `IActivityTestRunner` enables isolated single-activity testing, which is rare in workflow engines.

3. **Mature modularity and extensibility.** 50 independently deployable NuGet packages, 175 `*Feature*.cs` files with explicit `DependsOn` declarations, dual-layer core/implementation splits for persistence and resilience, and ConfigureAwait.Fody weaving as a compile-time cross-cutting concern all show architectural discipline applied consistently across the codebase.

**Most significant gaps (code-level):**

1. **Password hashing uses SHA-256 + salt rather than an adaptive slow hash.** `DefaultSecretHasher` uses `SHA256.Create()` with a 32-byte random salt. SHA-256 is a fast cryptographic hash — at current GPU speeds, it is insufficiently slow for password storage. BCrypt, Argon2, or PBKDF2 with a high iteration count is the current security best practice. This affects user passwords and application secrets. This finding should be assessed and resolved by a security engineer.

2. **No OpenTelemetry distributed tracing instrumentation in the workflow execution pipeline.** Despite a Datadog/OTel Docker Compose file suggesting intent, no `ActivitySource` or `System.Diagnostics.Activity` spans are emitted within the workflow or activity execution pipeline. This means distributed traces will not show workflow-level spans, limiting observability in complex microservice deployments and reducing analysability under production load.

3. **Rate limiting absent at the API ingress layer.** No ASP.NET Core `RateLimiter` middleware was observed in `Program.cs` or any module feature, and HTTP workflow trigger endpoints (potentially public-facing) are not rate-limited in the default configuration. This is a safety and security exposure: a public HTTP endpoint could be used to trigger unbounded workflow instantiation without throttling.

---

### Attributes not assessable from code and documentation alone
The following were assessed with Very low or Low confidence and require operational evidence:

- **Performance efficiency / Capacity** — requires load test results and per-node concurrency benchmarks
- **Reliability / Availability** — requires uptime metrics, SLA measurement, and production deployment review
- **Reliability / Faultlessness** — requires production error rate data and test coverage measurement
- **Reliability / Recoverability** — requires failure injection testing (process crash mid-execution)
- **Flexibility / Scalability** — requires horizontal scale-out load test with distributed lock provider
- **Safety (all sub-characteristics)** — requires domain-specific risk assessment by workflow designers and operators
- **Interaction capability / User engagement** — requires developer community research and adoption metrics
- **Compatibility / Co-existence** — requires deployment testing alongside representative co-hosted middleware stacks

---

### Final approval

| Field | Value |
|---|---|
| **Review completed by** | ___ |
| **Review date (UTC)** | ___ |
| **Verdict fields complete** | Yes / No — [n of 40 completed] |
| **Human completion checklists worked through** | Yes / Partial / No |
| **Approved as final report** | Yes / No |
| **Conditions or caveats** | ___ |