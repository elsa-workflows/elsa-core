# Extension Guide

Elsa is designed to be extended by adding modules, features, activities, stores, expression providers, API endpoints, and runtime integration points. This page collects the common patterns.

## Add A Code-First Feature

1. Create `Features/MyFeature.cs`.
2. Derive from `FeatureBase`.
3. Add `[DependsOn]` attributes for required features.
4. Use `Configure()` for feature graph changes and activity scanning.
5. Use `ConfigureHostedServices()` for hosted services.
6. Use `Apply()` for service registration.
7. Add `Extensions/ModuleExtensions.cs` with `UseMyFeature`.
8. Add unit tests that prove core services register.

Use [HttpFeature](../../src/modules/Elsa.Http/Features/HttpFeature.cs), [SchedulingFeature](../../src/modules/Elsa.Scheduling/Features/SchedulingFeature.cs), and [StructuredLogsFeature](../../src/modules/Elsa.Diagnostics.StructuredLogs/Features/StructuredLogsFeature.cs) as examples.

## Add A Shell Feature

Shell features live in `ShellFeatures` and implement CShells interfaces such as `IShellFeature`, `IFastEndpointsShellFeature`, or `IMiddlewareShellFeature`.

Use shell features when modular server/package configuration needs to activate the feature without code-first `AddElsa` calls.

Examples:

- [Elsa.Shells.Api/ShellFeatures/ShellsApiFeature.cs](../../src/modules/Elsa.Shells.Api/ShellFeatures/ShellsApiFeature.cs)
- [Elsa.Diagnostics.StructuredLogs/ShellFeatures/StructuredLogsFeature.cs](../../src/modules/Elsa.Diagnostics.StructuredLogs/ShellFeatures/StructuredLogsFeature.cs)
- [EF Core provider shell features](../../src/modules/Elsa.Persistence.EFCore.Sqlite/ShellFeatures)

## Add An Activity

1. Put the activity in the owning module's `Activities` folder.
2. Derive from `Activity`, `Activity<T>`, `CodeActivity`, or a module-specific base.
3. Use `Input<T>` and `Output<T>` for designer/runtime compatibility.
4. Register the activity with workflow management.
5. Add descriptor/UI hint handlers if the designer needs dynamic options.
6. Test activity-only behavior with `ActivityTestFixture`.
7. Add integration/component tests for bookmarks, triggers, persistence, or transport behavior.

Good examples:

- [SetVariable](../../src/modules/Elsa.Workflows.Core/Activities/SetVariable.cs)
- [HttpEndpoint](../../src/modules/Elsa.Http/Activities/HttpEndpoint.cs)
- [RunCSharp](../../src/modules/Elsa.Expressions.CSharp/Activities/RunCSharp/RunCSharp.cs)

## Add An Expression Provider

Expression providers need:

- evaluator contract and implementation
- expression descriptor provider
- optional activity for running scripts
- optional UI hint handler
- option type
- feature registration
- tests for evaluator behavior and workflow integration

Compare existing language modules:

- [JavaScriptFeature](../../src/modules/Elsa.Expressions.JavaScript/Features/JavaScriptFeature.cs)
- [CSharpFeature](../../src/modules/Elsa.Expressions.CSharp/Features/CSharpFeature.cs)
- [PythonFeature](../../src/modules/Elsa.Expressions.Python/Features/PythonFeature.cs)
- [LiquidFeature](../../src/modules/Elsa.Expressions.Liquid/Features/LiquidFeature.cs)

## Add An API Endpoint

1. Create a folder under the relevant `Endpoints` category.
2. Add `Endpoint.cs` and local `Models.cs` when needed.
3. Derive from the Elsa endpoint base used by nearby endpoints.
4. Configure verb, route, permissions, and summary.
5. Inject service contracts, not concrete internals when possible.
6. Add endpoint tests or component coverage if behavior is important.
7. Update client models if the endpoint is part of the public client surface.

Use route prefixing from `MapWorkflowsApi`; endpoint routes should usually be written without `/elsa/api`.

## Add A Store Or Persistence Provider

For an EF Core-backed store:

1. Add the entity/configuration/store to the shared EF Core module slice if it is a common Elsa domain.
2. Add provider-specific migrations if persisted shape changes.
3. Replace the owning feature's store factory in the persistence feature.
4. Add tests for query/filter/order behavior.
5. Add provider integration tests for migrations or SQL differences.

For diagnostics structured log relational providers:

1. Reference the relational structured-log package.
2. Implement `IRelationalStructuredLogConnectionFactory`.
3. Implement `IRelationalStructuredLogDialect`.
4. Implement `IStructuredLogSchemaMigrator`.
5. Register those services and call `AddRelationalStructuredLogPersistence`.

See [SqliteStructuredLogsModuleExtensions](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Extensions/SqliteStructuredLogsModuleExtensions.cs).

## Add A Runtime Ingress Source

External event sources should participate in graceful shutdown. Add an ingress source when a module feeds work into the runtime from outside the engine.

Steps:

1. Implement the runtime `IIngressSource` contract in the owning module.
2. Register it as a singleton service.
3. Make dispatch loops or middleware honor paused state.
4. Add tests for pause/resume/drain behavior.

Existing first-party examples are HTTP and Scheduling ingress source registrations.

## Add A Workflow Provider

Workflow providers bring definitions from external storage. Existing examples:

- [Elsa.WorkflowProviders.BlobStorage](../../src/modules/Elsa.WorkflowProviders.BlobStorage)
- [Elsa.WorkflowProviders.BlobStorage.ElsaScript](../../src/modules/Elsa.WorkflowProviders.BlobStorage.ElsaScript)

Keep provider modules focused on discovery/materialization and leave execution to runtime.

## Add Documentation

Update docs when changing:

- public APIs
- options/configuration
- endpoint routes
- persistence schema or provider setup
- runtime behavior
- security/authorization behavior
- developer workflows

Good locations:

- module README
- relevant spec quickstart
- this wiki
- ADR for architectural decisions

## Design Rules Of Thumb

- Keep core provider-neutral.
- Use feature configuration instead of direct cross-module service replacement.
- Add dependencies explicitly with `[DependsOn]`.
- Prefer contracts at module boundaries.
- Put tests near the module whose behavior changed.
- Avoid adding shared abstractions until at least two real modules need them.
