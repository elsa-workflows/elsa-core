<!--
  Sync Impact Report
  ===================
  Version change: 1.0.1 → 1.1.0 (MINOR — materially expanded guidance under
  existing Principle VII; adds named SRP/DRY/KISS sub-rules and an explicit
  conciseness rule that previously was implicit.)

  Modified principles:
    - VII. Simplicity & Focus → VII. Simplicity, SRP, DRY & KISS
        - Existing rules retained (YAGNI, direct-over-indirect, singular-
          purpose modules, proportional error handling).
        - Added explicit SRP rule: classes/modules/functions have one reason
          to change; multi-concern classes MUST be split.
        - Added explicit DRY rule with rule-of-three guidance to prevent
          premature abstraction of incidentally-similar code.
        - Added explicit KISS framing of the existing simplicity rules.
        - Added explicit conciseness rule covering design docs, plan/spec
          artifacts, and code (one well-named function over pass-through
          chains; comments explain WHY not WHAT).

  Added principles: none
  Added sections: none
  Removed sections: none

  Templates reviewed:
    - .specify/templates/plan-template.md         ✅ compatible (Constitution
      Check gate is generic; new sub-rules surface naturally during review)
    - .specify/templates/spec-template.md         ✅ compatible
    - .specify/templates/tasks-template.md        ✅ compatible
    - .specify/templates/checklist-template.md    ✅ compatible

  Runtime guidance reviewed:
    - .github/copilot-instructions.md             ✅ no Principle-VII references
    - CLAUDE.md                                   ✅ no Principle-VII references
    - README.md                                   ✅ no Principle-VII references

  Follow-up TODOs: none
-->

# Elsa Workflows Constitution

## Core Principles

### I. Modular Architecture

Every feature area MUST be implemented as an independent module (separate .csproj) with clear boundaries.

- Modules reside under `src/modules/` and MUST be self-contained with their own
  DI registration, contracts, and implementations.
- Each module MUST follow the standard directory structure: `ShellFeatures/`,
  `Contracts/`, `Services/`, `Extensions/`, `Models/`, and optionally
  `Activities/`, `Middleware/`, `Handlers/`.
- No module may directly reference another module's internal types. Cross-module
  communication MUST go through published contracts (interfaces) or the mediator.
- Shared infrastructure belongs in `src/common/`; application hosts belong in
  `src/apps/`.

**Rationale**: Modularity enables independent development, testing, and
deployment of features. It keeps the 100+ project solution manageable and allows
consumers to depend only on the modules they need via NuGet.

### II. Composition & Extensibility

The system MUST favour composition over inheritance and provide explicit
extensibility points for every major subsystem.

- Features register via Shell Features (`IShellFeature`, `IMiddlewareShellFeature`)
  with dependency declarations through the `[ShellFeature(DependsOn = [...])]`
  attribute. The shell resolves and initialises features in correct order.
- Activities are the composable building blocks of workflows. New activities
  MUST extend `Activity`, `Activity<T>`, `CodeActivity`, or `CodeActivity<T>`
  and declare metadata via `[Activity]`, `[Input]`, and `[Output]` attributes.
- Persistence MUST be provider-agnostic. Store interfaces (e.g.,
  `IWorkflowDefinitionStore`) define the contract; EF Core implementations
  are one provider among potentially many.
- Expression evaluation MUST be pluggable via `IExpressionEvaluator`
  implementations (JavaScript, C#, Python, Liquid).
- Service registration MUST use `Microsoft.Extensions.DependencyInjection`
  with fluent `IServiceCollection` extension methods per module.

**Rationale**: Elsa is a library embedded in arbitrary .NET hosts. Users MUST
be able to swap persistence, expression engines, and feature sets without
modifying core code.

### III. Convention-Driven Design

The codebase MUST use consistent, discoverable conventions to reduce cognitive
load and enable tooling automation.

- Activity metadata MUST be declared via attributes (`[Activity]`, `[Input]`,
  `[Output]`) — not via external configuration.
- Inputs and outputs MUST use the typed wrappers `Input<T>` and `Output<T>`
  for expression-capable, type-safe data flow.
- API endpoints MUST follow the single-endpoint-per-class pattern
  (`ElsaEndpoint<TRequest, TResponse>`) with collocated request/response models.
- Naming conventions:
  - Feature classes: `{Area}Feature` (e.g., `HttpFeature`, `WorkflowsFeature`)
  - Store interfaces: `I{Entity}Store`
  - Extension methods: `{Module}ServiceCollectionExtensions`
  - Test classes: `{Subject}Tests`
- C# latest language version, nullable reference types enabled, implicit usings
  enabled — enforced via `Directory.Build.props`.
- **Spelling & language**: All new code, comments, identifiers, error messages,
  XML doc, commit messages, and Speckit artifacts (spec.md, plan.md, research.md,
  tasks.md, checklists, etc.) MUST use American English (e.g., `materialize`,
  `behavior`, `analyze`, `color`, `center`). Existing public API symbols that
  follow another convention (e.g., `WorkflowSubStatus.Cancelled` with double-L,
  established by .NET / Elsa convention) are NOT renamed retroactively — keep
  them stable, but every newly-introduced symbol and every line of new prose
  MUST follow American English.

**Rationale**: Predictable patterns make the large codebase navigable and reduce
onboarding friction. Attribute-driven metadata enables automatic UI generation
in the visual designer. A single spelling convention prevents drift in
identifiers and search results across the 100+ project solution.

### IV. Async & Pipeline Execution

All I/O-bound and workflow execution paths MUST use async/await. Cross-cutting
concerns MUST be handled via middleware pipelines.

- Activity execution flows through `IActivityExecutionPipeline` — a composable
  middleware chain built with `IActivityExecutionPipelineBuilder`.
- Workflow execution uses `IWorkflowRunner` with scoped
  `ActivityExecutionContext` for state isolation.
- Internal messaging uses the custom `IMediator` (request/response and
  notification patterns) — not external libraries.
- `ValueTask` SHOULD be preferred over `Task` for hot execution paths where
  synchronous completion is common.

**Rationale**: Workflows are inherently asynchronous and long-running. Pipeline
architecture enables observability, resilience, and logging middleware without
polluting activity code.

### V. Testing Discipline

New code MUST include tests. Tests MUST be organised by scope and follow
established patterns.

- **Framework**: xUnit with `[Fact]` and `[Theory]` attributes.
- **Test organisation**: `test/unit/` for isolated tests, `test/integration/`
  for end-to-end scenarios, `test/component/` for feature testing.
- **Shared fixtures**: Use `ActivityTestFixture` for unit-testing activities
  and `WorkflowTestFixture` for integration tests.
- Test methods MUST use descriptive names; `DisplayName` SHOULD be set for
  readability.
- Tests MUST NOT depend on external services or network access unless
  explicitly categorised as integration tests.

**Rationale**: The workflow engine is the critical path for all consuming
applications. Regressions in core execution logic have outsized impact.

### VI. Trunk-Based Development

All changes MUST flow through Pull Requests targeting the `main` branch.

- One PR = one concern (bug fix, refactor, feature, docs, or dependency update).
  PRs that mix unrelated concerns MUST be split.
- PRs MUST include: clear problem statement, expected behaviour, steps to verify,
  and tests for new code.
- Small, focused PRs are preferred for faster review and safer merges.
- API documentation MUST be updated when APIs change.
- Code review is required before merge.

**Rationale**: Trunk-based development with focused PRs maintains velocity
while keeping the codebase stable for the large contributor community.

### VII. Simplicity, SRP, DRY & KISS

Architecture and code MUST be as concise as accuracy allows. Complexity MUST be
justified. SRP, DRY, and KISS MUST be applied wherever appropriate.

- **KISS (Keep It Simple)**: Start simple; avoid speculative generalization.
  YAGNI — do not add features, abstractions, or configuration for hypothetical
  future requirements. Prefer direct implementations over indirection layers
  unless the indirection serves a documented extensibility need.
- **SRP (Single Responsibility)**: Each class, module, and function MUST have
  one well-defined reason to change. Modules MUST have a clear, singular purpose
  — no "grab bag" utility modules without justification. A class that mixes
  persistence, validation, and orchestration concerns MUST be split.
- **DRY (Don't Repeat Yourself)**: Repeated logic, configuration, or knowledge
  SHOULD be extracted into a shared abstraction by the third occurrence (rule
  of three) — earlier when the duplication is structural rather than incidental.
  Do NOT prematurely DRY code that is only incidentally similar and likely to
  diverge; duplication is cheaper than the wrong abstraction.
- **Conciseness**: Design docs, Speckit artifacts (spec.md, plan.md, tasks.md,
  research.md, checklists), and code MUST be as short as accuracy allows.
  Prefer one well-named function over a chain of pass-through wrappers.
  Inline trivially small helpers rather than wrapping them in extra methods.
  Comments explain WHY (non-obvious constraints, invariants, workarounds), not
  WHAT — well-named identifiers already convey what.
- **Proportional error handling**: Validate at system boundaries (user input,
  external APIs); trust internal code and framework guarantees. Do not add
  defensive checks for conditions that cannot occur.

**Rationale**: A 100+ project solution is only sustainable when each part earns
its existence. SRP keeps modules navigable and testable; DRY keeps fixes in one
place; KISS keeps the system understandable to new contributors; conciseness
keeps reviews fast and intent legible. The rule-of-three guard on DRY prevents
premature abstraction — the inverse failure mode of duplication, and one that
is harder to undo once entrenched.

## Technology Stack & Constraints

- **Runtime**: .NET 10.0 (primary), with multi-target support for .NET 8.0
  and .NET 9.0 via `<TargetFrameworks>` in `src/Directory.Build.props`.
- **Language**: C# latest (`<LangVersion>latest</LangVersion>`).
- **Nullable reference types**: Enabled globally.
- **Implicit usings**: Enabled globally.
- **Build system**: NUKE build automation (`build/Build.cs`); CI runs
  `./build.cmd Compile Test Pack`.
- **Package management**: Central package management via
  `Directory.Packages.props`; NuGet source mapping in `NuGet.Config`.
- **Serialization**: System.Text.Json with custom converters and type
  registries; polymorphic serialisation via `IWellKnownTypeRegistry`.
- **Persistence**: EF Core with per-module `DbContext`; generic
  `Store<TDbContext, TEntity>` base class; supports SQL Server, PostgreSQL,
  MySQL, SQLite, Oracle.
- **Multi-tenancy**: `ITenantAccessor` with `AsyncLocal<Tenant?>` context;
  query filters for tenant isolation.
- **License**: MIT.
- **Docker**: Server, Studio, and combined images available under `docker/`.

## Development Workflow & Quality Gates

### Build

```bash
# Restore (always use --ignore-failed-sources for external feed resilience)
./build.sh Restore --ignore-failed-sources

# Compile (excludes studio apps)
./build.sh Compile

# Test
./build.sh Test

# Full CI pipeline
./build.sh Compile Test Pack
```

- NU1900/NU1801 warnings for external feeds are expected and safe to ignore.
- Individual modules can be built with `dotnet build src/modules/{Module}/`.

### Quality Gates for PRs

1. Code compiles without errors on all target frameworks.
2. All existing tests pass.
3. New code includes appropriate tests (unit and/or integration).
4. API changes include documentation updates.
5. PR is scoped to a single concern.
6. Code review approved by at least one maintainer.

### Commit Message Convention

Follow conventional commits: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`,
`test:`, `perf:`.

## Governance

This constitution is the authoritative reference for architectural decisions
and development practices in the Elsa Workflows repository.

- **Precedence**: This constitution supersedes ad-hoc conventions. When in
  conflict, this document governs.
- **Amendments**: Changes to this constitution MUST be documented with a
  version bump, rationale, and migration guidance where applicable.
- **Versioning**: Constitution versions follow semantic versioning:
  - MAJOR: Backward-incompatible principle removals or redefinitions.
  - MINOR: New principle or section added, materially expanded guidance.
  - PATCH: Clarifications, wording, typo fixes, non-semantic refinements.
- **Compliance review**: PRs and implementation plans SHOULD reference the
  Constitution Check section in `plan-template.md` to verify alignment.
- **Complexity justification**: Any deviation from these principles MUST be
  documented in the Complexity Tracking table of the implementation plan.
- **Runtime guidance**: See `.github/copilot-instructions.md` for build
  commands, troubleshooting, and CI details.

**Version**: 1.1.0 | **Ratified**: 2026-03-08 | **Last Amended**: 2026-05-01
