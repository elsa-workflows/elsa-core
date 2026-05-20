# Elsa Workflows Core — Software quality scan

| Field | Value |
|---|---|
| **Repository / project** | elsa-workflows/elsa-core |
| **Git ref** | release/3.7.0 |
| **Version** | 3.7.0 |
| **Assessment date (UTC)** | 2026-05-20T00:00:00Z |
| **Assessment method** | Static code analysis, Software quality scan |
| **Model and tools** | Claude Sonnet 4.6 · Static code analysis · Software Quality Catalog v0.1 |
| **Assessment scope** | All source projects in src/ (50 projects) and test/ (23 projects) |
| **Related documents** | elsa-core-profile.md, elsa-core-architecture-patterns.md, elsa-core-iso25010.md |

> ⚠️ **AI-assisted assessment — human review required**
>
> This document was produced by an AI model (Claude Sonnet 4.6) using static code analysis and structured quality catalogs. Findings are derived from static pattern detection across source files. They identify signals that warrant human judgment; they are not definitive defect reports. Severity ratings reflect the risk profile of each pattern, not confirmed impact.
>
> **This document is a draft input to a human review process — not a final approved report.**
> All findings should be verified by a qualified engineer familiar with the codebase before being acted upon.

---

## 9. Software quality scan

**Scan date:** 2026-05-20
**Scope:** All source projects in src/ (50 projects) and test/ (23 projects)
**Coverage report:** Not present — Coverlet/OpenCover XML not found in repository

> Assessment status key:
> ✅ Fully assessed — systematic, deterministic finding
> 🟡 Partially assessed — pattern-detectable, needs context to interpret
> 🔴 Judgment required — heuristic; human review needed

---

### 9.1 OOP and component design

| Practice | Status | Finding | Recommendation |
|---|---|---|---|
| Single Responsibility | 🔴 | `ActivityExecutionContext` (818 lines, ~94 member declarations) and `WorkflowExecutionContext` (716 lines, ~53 method-level members) both combine scheduling, memory, property bag, bookmark management, service location, and state transition responsibilities in a single class. Each exhibits 5–6 distinct concern clusters detectable by field grouping. `HttpEndpoint` (520 lines) merges trigger registration, request validation, file upload handling, and MIME-type enforcement into one activity class. | Decompose `ActivityExecutionContext` and `WorkflowExecutionContext` into focused collaborators (e.g. separate bookmark manager, memory accessor, service locator façade). Extract file and MIME validation out of `HttpEndpoint` into a dedicated request-validation pipeline. |
| Open/Closed | 🟡 | No systemic `switch`/`if-else` on type strings found in the workflow core. The codebase relies extensively on interfaces, middleware pipelines, and strategy patterns to extend behaviour. `DefaultIncidentStrategyResolver` and `DefaultExpressionDescriptorProvider` use type-based dispatch but with polymorphic lookup rather than raw string comparisons — acceptable OCP practice. | No action required for the core. Monitor `ActivityJsonConverter`, which deserialises by type name string and contains one custom fallback branch; ensure new activity types are registered rather than hard-coded. |
| Liskov Substitution | 🟡 | 17 `throw new NotImplementedException()` calls found project-wide. In production code: `JsonIgnoreCompositeRootConverter.Read` (intentional — write-only converter), `RootActivityNodeConverter.Read`, `DownloadableContentHandlerBase.HandleAsync`, and `HttpStatusCodeCaseForWorkflowInstanceConverter.Write` — all are deliberate narrow contracts. No LSP violation in primary workflow execution path detected. | Mark narrow contracts with `[Obsolete]` or XML doc warnings rather than silent `NotImplementedException`; consider sealed/abstract partial implementations to enforce the intent. |
| Interface Segregation | 🟡 | `IWorkflowBuilder` has 28+ methods covering both fluent mutation and workflow-construction concerns — a moderately fat interface. `IActivity` is lean (2 methods + 6 properties) — well-segregated. `IActivityRegistry` exposes both registration (write) and lookup (read) operations in a single contract. | Split `IWorkflowBuilder` into `IWorkflowBuilderConfiguration` (mutating builder setters) and `IWorkflowBuilderFactory` (build-time methods). Consider splitting `IActivityRegistry` into `IActivityRegistryWriter` and `IActivityRegistryReader`. |
| Dependency Inversion | ✅ | Constructor injection is consistent throughout the codebase. All sampled services (`ActivityRegistry`, `WorkflowHost`, `ActivityDescriber`, `WorkflowGraphBuilder`) receive only interfaces or abstractions in their constructors. `new ConcreteType()` in class bodies is confined to value objects, DTOs, and intentional in-method command construction (e.g. `BackgroundWorkflowDispatcher` creates command records, not services). | No action required. DI discipline is strong. |
| God classes | 🟡 | `ActivityExecutionContext` (818 lines, 94 member declarations) and `WorkflowExecutionContext` (716 lines, 53 method members) exceed the god-class threshold on both line count and method count. `Store<TDbContext,TEntity>` (635 lines) is a broad generic repository aggregating Add, AddMany, Save, Update, Delete, Count, Query, and Find operations. `HttpEndpoint` (520 lines) is a heavyweight activity. | See SRP recommendation above. For `Store`, consider splitting bulk-operation helpers into extension methods or a dedicated `BulkStore` specialisation. |
| Primitive obsession | 🔴 | `WorkflowExecutionContext` constructor accepts `string? correlationId`, `string? parentWorkflowInstanceId`, `string? triggerActivityId` — three adjacent `string?` parameters with similar shapes and semantics. `ActivityIncident` constructor takes five `string` parameters. `Bookmark` default constructor passes six string literals. These are latent swap-argument bugs and hinder readability. | Introduce typed value objects: `CorrelationId`, `WorkflowInstanceId`, `TriggerActivityId`. A source generator or record wrapper is sufficient. |

---

### 9.2 Coupling and cohesion

| Practice | Status | Finding | Recommendation |
|---|---|---|---|
| Afferent / efferent coupling | ✅ | `ActivityExecutionContext` is the highest-fan-in class in the codebase — referenced by virtually every module through its public API surface (activities, middleware, extensions, services). `WorkflowExecutionContext` is the second. Both are intentional hub types. `Elsa.Workflows.Core` has very high efferent coupling (30+ using directives in key files) as the foundational module — all other modules depend on it; it depends on no higher-level modules. | The coupling topology is architecturally justified for a workflow engine. However, the extension-method files (`ActivityExecutionContextExtensions.cs` at 520 lines, `ExpressionExecutionContextExtensions.cs` at 570 lines) should be reviewed for methods that could be inlined or moved to feature modules to reduce artificial coupling. |
| Cyclic dependencies | ✅ | No assembly-level cycles detected. `Elsa.Workflows.Core` → `Elsa.Common` → no Elsa.Workflows reference. `Elsa.Workflows.Runtime` → `Elsa.Workflows.Core` is the expected layering. `Elsa.Workflows.Api` depends on `Elsa.Workflows.Management` and `Elsa.Workflows.Runtime` but neither depends back on the API layer. | No action required. |
| LCOM (Lack of Cohesion of Methods) | 🔴 | `ActivityExecutionContext` contains groups of methods that share no common fields: service-location methods (`GetRequiredService`, `GetOrCreateService`) reference only `WorkflowExecutionContext`; bookmark methods reference `_newBookmarks`; property bag methods reference `Properties`; memory-block methods reference `ExpressionExecutionContext`. These clusters indicate low cohesion — a god-class symptom. Similar pattern in `WorkflowExecutionContext`. | Decompose along cohesion boundaries. A `IActivityServiceLocator`, `IActivityBookmarkManager`, and `IActivityMemoryAccessor` wrapping the context would restore cohesion. |
| Law of Demeter | 🟡 | Several extension methods chain 3+ hops: `context.WorkflowExecutionContext.Workflow.Options.CommitStrategyName` (`DefaultActivityInvokerMiddleware`), `context.WorkflowExecutionContext.Workflow.Options.IncidentStrategyType` (`DefaultIncidentStrategyResolver`). These cross three object boundaries. Fewer than 10 locations detected. | Introduce façade properties on `WorkflowExecutionContext` (`CommitStrategyName`, `IncidentStrategyType`) that delegate internally, hiding the traversal. |
| Tell, don't ask | 🟡 | `WorkflowHost.RunWorkflowAsync` checks `WorkflowState.Status != WorkflowStatus.Running` before delegating — query-then-act on the same object. The pattern is isolated to the deprecated `WorkflowHost` class. Core workflow execution pipelines use tell-style scheduling (`ScheduleActivityAsync`). | Low priority given the `[Obsolete]` marker on `WorkflowHost`. No action required in new code. |
| Feature envy | 🟡 | `ActivityExecutionContextExtensions.InputEvaluation.cs` and `ActivityExecutionContextExtensions.cs` (520 lines combined) contain methods whose logic primarily manipulates the state of `ActivityExecutionContext` — suggesting the behaviour belongs on the class itself. This is a known .NET pattern for keeping context classes from growing, but the extensions are so numerous they become de facto class members. | Audit which extensions are called only from within a single activity and move them to protected helpers on `Activity` base classes. |

---

### 9.3 Code complexity and size

| Practice | Status | Threshold | Violations | Worst offender |
|---|---|---|---|---|
| Cyclomatic complexity | 🟡 | >10 warn, >20 severe | ~3–5 methods at warn level, 1–2 at severe | `HttpEndpoint.HandleRequestAsync` (multiple nested if/return paths, estimated CC ~14); `ActivityDescriber.DescribeActivityAsync` (multiple LINQ + conditional branches, estimated CC ~12) |
| Method length | ✅ | >50 lines warn, >100 severe | ~4 methods exceed 50 lines in non-generated production code | `HttpEndpoint.HandleRequestAsync` (~88 lines); `WorkflowExecutionContext.CreateAsync` overloads (~50 lines each); `ActivityDescriber.DescribeActivityAsync` (~70 lines) |
| Class length | ✅ | >300 lines warn, >700 severe | 4 at warn; 2 at severe | **Severe:** `ActivityExecutionContext` (818 lines), `WorkflowExecutionContext` (716 lines). **Warn:** `Store<TDbContext,TEntity>` (635 lines), `ExpressionExecutionContextExtensions` (570 lines), `ActivityExecutionContextExtensions` (520 lines), `HttpEndpoint` (520 lines). EF Core migration-generated files excluded. |
| Nesting depth | 🟡 | >4 levels | ~2–3 locations at depth 4–5 | `HttpEndpoint.HandleRequestAsync` nesting depth ~5 (method → try → if → if → if); `TenantTaskManager` catch blocks reach ~4 levels inside async lambdas |
| Parameter count | ✅ | >5 params | 3 constructors | `WorkflowExecutionContext` private constructor (13 parameters); `Workflow` constructor (12 parameters); `ActivityIncident` constructor (6 parameters). All use named parameters at call sites, reducing swap risk, but the 13-parameter constructor is a complexity indicator. |

---

### 9.4 Code hygiene

| Practice | Status | Finding |
|---|---|---|
| Code duplication (DRY) | 🟡 | File-validation methods in `HttpEndpoint` (`ValidateFileSizes`, `ValidateFileExtensionWhitelist`, `ValidateFileExtensionBlacklist`, `ValidateFileMimeTypes`) share a repeated structure: check limit input → return true if not configured → evaluate collection → set response status code 413 or 415. This structural repetition (~15 lines each) could be extracted into a generic validation pipeline. EF Core migration files (15+ generated files, 500+ lines each) excluded. |
| Dead code | 🟡 | `PropertyOptionsResolver.cs` contains ~8 lines of commented-out constructor injection code. `PersistentVariableState.cs` has 5 lines of commented-out class body. `ActivityExecutionContextExtensions.InputEvaluation.cs` has 4 lines of commented-out log sanitisation code with a TODO. `ActivitySchedulerFactory.cs` contains `//public IActivityScheduler CreateScheduler() => new StackBasedActivityScheduler();`. These are relics of active migration and not dead production paths, but they accumulate noise. |
| Magic literals | 🟡 | `BackgroundActivityExecutionContextExtensions` uses string keys `"BackgroundCompletion"`, `"BackgroundScheduledActivities"` as property bag keys directly in multiple methods. `Switch.cs` uses `"ScheduledActivityIds"` as a property key. These magic strings are repeated across get/set pairs and risk typo-based bugs. `WorkflowStorageDriver` defines a `const string` for its key — the correct pattern. |
| TODO / FIXME comments | ✅ | 12 TODO comments found in production source (0 FIXME/HACK). Notable items: `DefaultAlterationRunner.cs` — architectural concern about double-save on DB; `WorkflowStateExtractor.cs` — temporary solution acknowledged; `HttpEndpoint/WriteFileHttpResponse.cs` — cached file not deleted; `Elsa.Http/DownloadableContentHandlers` — file caching not implemented; `NotificationLoggingMiddleware.cs` — logging stub. None are in critical execution paths but several reflect unimplemented features. |
| Commented-out code | 🟡 | 6 distinct blocks of commented-out code found: `PropertyOptionsResolver.cs` (8 lines), `PersistentVariableState.cs` (5 lines), `ActivityExecutionContextExtensions.InputEvaluation.cs` (4 lines with TODO), `ActivitySchedulerFactory.cs` (1 line), `IActivityPropertyOptionsProvider.cs` (1 commented method signature). Benign but should be removed or converted to tracked issues. |
| Naming conventions | 🟡 | Integration test methods named `Test1`, `Test2`, `Test3` found in 8+ test files (`ToJsonTests.cs`, `JsonConverterTest.cs`, `MigrationTests.cs`, `SetGetVariables/Tests.cs`, `JavaScriptListsAndArrays/Tests.cs`, `JavaScriptNativeVariables/Tests.cs`, `JsonObjectSerialization/Tests.cs`, `WorkflowDefinitionStorePopulation/Tests.cs`). Production code naming is consistent and idiomatic. No single-character variables in production paths. |

---

### 9.5 Testability and test quality

| Practice | Status | Finding |
|---|---|---|
| Dependency injection usage | ✅ | Consistent constructor injection throughout all sampled service and activity classes. `new ConcreteType()` in service bodies is limited to command/request record creation (e.g. `new DispatchWorkflowDefinitionCommand(...)` in `BackgroundWorkflowDispatcher`) — data-only DTOs, not services. No `new ConcreteService()` anti-pattern found in class bodies. |
| Test coverage | ✅ | Coverage report not present in repository — cannot assess line/branch coverage numerically. |
| Test assertion quality | 🟡 | Unit tests in `Elsa.Activities.UnitTests` (e.g. `IfTests.cs`) are exemplary: each `[Fact]` has 2–4 `Assert.*` calls with descriptive failure messages, follows Arrange-Act-Assert, uses `[Theory]/[InlineData]` for parameterised cases, and tests edge cases. Integration tests show mixed quality: tests named `Test1`/`Test2` in `SetGetVariables/Tests.cs` have `[Fact(DisplayName = "...")]` which partially mitigates the opaque name, but `ToJsonTests.cs` and `JsonConverterTest.cs` use bare `Test1`/`Test2` names without display names. |
| Test pyramid shape | ✅ | 12 unit test projects, 8 integration test projects, 1 component test project, 1 performance test project. Unit tests significantly outnumber integration tests — healthy pyramid shape. The ~530-test total (by `[Fact]/[Theory]` count) is concentrated in unit tests (~78 files) vs integration tests (~8 files with assertions). |
| Test isolation | 🟡 | `static readonly DefinitionId = Guid.NewGuid().ToString()` found in 4+ integration test workflow classes. These are set at class-load time and are effectively constant per test run — not mutated during tests, so they do not introduce state contamination. `TestSettings.IncidentStrategyType` is a `static` mutable property in `test/integration/.../Incidents/Statics/TestSettings.cs`, which could cause ordering-dependent failures if tests set it concurrently. No `Thread.Sleep` found in test files. |

---

### 9.6 API and contract design

| Practice | Status | Finding |
|---|---|---|
| HTTP status code semantics | 🟡 | `HttpEndpoint` correctly uses `StatusCodes.Status413PayloadTooLarge` for size violations, `StatusCodes.Status415UnsupportedMediaType` for invalid MIME types/extensions, and `StatusCodes.Status400BadRequest` for invalid JSON payloads. Status codes are semantically accurate. No 200-for-error or 403-for-validation misuse detected in sampled endpoints. |
| Response shape consistency | 🟡 | Error responses in `HttpEndpoint` write anonymous `{ Message = "..." }` JSON objects directly — not a shared typed error envelope. `Elsa.Workflows.Api` endpoints use FastEndpoints-style `Request`/`Response` pairs. No global error schema was found to be enforced. Inconsistency risk exists between workflow endpoint responses and HTTP activity error responses. |
| Configuration externalisation | 🟡 | `appsettings.json` in `Elsa.Server.Web` contains a default JWT signing key `"sufficiently-large-secret-signing-key"`, three hashed user passwords, and an application client secret. This is a development/demo configuration file. All values are externalisable via `IConfiguration` (the `Program.cs` correctly binds identity sections from configuration). The `DefaultConnectionString` constant in `Elsa.Persistence.EFCore.Common/Constants.cs` (`"Data Source=elsa.sqlite.db;Cache=Shared;"`) is a fallback default — acceptable for development, must be overridden in production. |
| Null handling discipline | ✅ | `<Nullable>enable</Nullable>` is set globally in `Directory.Build.props`. All sampled interfaces use `string?` for optional members and non-nullable for required members. `WorkflowExecutionContext` constructor uses `IDictionary<string, object>?` with explicit null-coalescing. No `#nullable disable` overrides found in sampled files. Nullable discipline is strong. |

---

### 9.7 Operational quality

| Practice | Status | Finding |
|---|---|---|
| Structured logging | 🟡 | The vast majority of log calls correctly use message templates with named placeholders. Two violations found: `Elsa.Mediator/Services/JobQueue.cs` line 26: `logger.LogWarning($"Job {jobId} was not found")` — uses string interpolation instead of a structured template. `Elsa.Server.Web/ActivityHosts/Penguin.cs` line 35: `logger.LogInformation($"The penguin is eating {food}!")` — sample app shipped in repo. Both defeat semantic log querying and cause string allocation before log-level check under high load. |
| Exception handling | 🟡 | `TenantTaskManager` uses `catch (Exception e) when (!e.IsFatal())` with logging and non-rethrow — appropriate for background recurring tasks. Two **empty** `catch (Exception)` blocks found in `DefaultExpressionDescriptorProvider.cs`: expression parsing failures silently return a fallback expression with no log call. `ObjectConverter.cs` has two broad `catch (Exception e)` blocks for type-conversion fallback — reasonable context, but no log entry is produced for conversion failures. |
| Async/await correctness | ✅ | `ConfigureAwait.Fody` is in use (confirmed by `FodyWeavers.xml` in multiple modules) — `ConfigureAwait(false)` is injected automatically at build time; manual calls not required. No `.Result` blocking calls on live `Task`/`ValueTask` instances found (the `t.Result` in `BulkCancel/Endpoint.cs` accesses a completed `Task<int>` after `await Task.WhenAll(tasks)` — safe). Three `async void` methods found: `ScheduledTimer.Callback`, `HeartbeatGenerator.GenerateHeartbeatAsync`, `ConfigurationTenantsProvider.OnOptionsChanged` — all are timer or `IOptionsMonitor` callbacks where `async void` is the required signature, each wrapping a try/catch. |
| Disposable resource handling | 🟡 | `IDisposable` implementations are consistently provided wherever `IDisposable` is implemented (`TenantScope`, `DefaultTenantService`, `ScheduledTimer`, `WorkflowHost`). `CancellationTokenSource` objects are disposed after use in `WorkflowHost.RunWorkflowAsync`. No `new HttpClient()` per-request instantiation detected. `static readonly SemaphoreSlim Semaphore = new(1, 1)` in `Store<TDbContext, TEntity>` is a static disposable that is never disposed — acceptable for application-lifetime singletons, but worth noting. |
| Security hygiene | 🟡 | **SHA-256 for password hashing:** `DefaultSecretHasher` uses `SHA256.Create()` with a concatenated password+salt. SHA-256 is not a password-hashing algorithm (it is fast by design), making offline dictionary attacks and rainbow-table attacks feasible. Replace with PBKDF2 (`Rfc2898DeriveBytes`, ≥100,000 iterations), bcrypt, or Argon2id. **Committed default signing key:** `appsettings.json` contains `"SigningKey": "sufficiently-large-secret-signing-key"` in the server sample app. **Raw SQL construction:** `BulkUpsertExtensions.cs` constructs SQL strings using `$"INSERT INTO \"{tableName}\""` with table/column names sourced from EF Core metadata (not user input) — low injection risk currently but warrants ongoing review. No API keys or secrets found embedded in `.cs` files. |

---

### 9.8 Quality scan summary

| Category | Checks run | ✅ Clean | 🟡 Needs review | 🔴 Action required |
|---|---|---|---|---|
| 9.1 OOP and component design | 7 | 1 | 3 | 3 |
| 9.2 Coupling and cohesion | 6 | 2 | 3 | 1 |
| 9.3 Code complexity and size | 5 | 2 | 2 | 1 |
| 9.4 Code hygiene | 6 | 1 | 4 | 1 |
| 9.5 Testability and test quality | 5 | 2 | 2 | 0 |
| 9.6 API and contract design | 4 | 2 | 2 | 0 |
| 9.7 Operational quality | 5 | 1 | 4 | 0 |
| **Total** | **38** | **11** | **20** | **7** |

**Top 3 priority findings:**

1. **SHA-256 used for password hashing in `DefaultSecretHasher`** (`src/modules/Elsa.Identity/Services/DefaultSecretHasher.cs`). SHA-256 is a fast general-purpose hash, not a key-derivation function. It makes offline dictionary attacks and rainbow-table attacks feasible. Replace with `Rfc2898DeriveBytes` (PBKDF2-SHA256, ≥100,000 iterations), `BCrypt.Net`, or `Konscious.Security.Cryptography` (Argon2id). Affects all deployments using the built-in identity provider.

2. **God-class `ActivityExecutionContext` and `WorkflowExecutionContext`** (`src/modules/Elsa.Workflows.Core/Contexts/`). At 818 and 716 lines respectively, with 94 and 53 member declarations, both classes accumulate scheduling, memory, bookmarks, service location, state transitions, and metadata management. Low cohesion and very high afferent coupling make them the highest-risk classes for regression on any change. Decomposition into focused collaborators (bookmark manager, memory accessor, service-location façade) would reduce change-blast-radius and improve testability.

3. **Development signing key and credentials committed in `appsettings.json`** (`src/apps/Elsa.Server.Web/appsettings.json`). The JWT signing key `"sufficiently-large-secret-signing-key"` and three user credential entries are committed to source control. If accidentally deployed to production without environment-specific overrides, this presents an authentication bypass risk. Add an explicit `.gitignore` rule for `appsettings.Production.json`, document the requirement to override `Identity:Tokens:SigningKey` via secrets management (Azure Key Vault, AWS Secrets Manager, Docker secrets), and remove all user entries from the committed config in favour of runtime-only seeding.

---

**Not assessed in this scan:**
- Modularity and module-level cohesion — requires architectural review of assembly dependency graph tooling
- Whether SOLID violations are design intent or mistakes — requires domain knowledge of the workflow engine's evolution
- Test correctness (whether tests verify the right behaviour) — requires domain review
- Cyclomatic complexity exact counts — requires running a static analysis tool; estimates above are from manual inspection
