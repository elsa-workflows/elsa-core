# Activities And Authoring

Elsa supports several workflow authoring paths: C# workflow classes, JSON definitions, visual designer definitions, host method activities, workflow-definition activities, and the experimental ElsaScript DSL.

## C# Workflows

C# workflows usually derive from [WorkflowBase](../../src/modules/Elsa.Workflows.Core/Abstractions/WorkflowBase.cs) and implement `Build(IWorkflowBuilder builder)`.

The root activity can be a `Sequence`, `Flowchart`, or another composite activity. The README has a minimal example that starts with `HttpEndpoint` and then sends email.

Source landmarks:

- [WorkflowBase](../../src/modules/Elsa.Workflows.Core/Abstractions/WorkflowBase.cs)
- [WorkflowBuilder](../../src/modules/Elsa.Workflows.Core/Builders/WorkflowBuilder.cs)
- [IWorkflowBuilder](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowBuilder.cs)
- [Workflow runtime feature AddWorkflow/AddWorkflowsFrom](../../src/modules/Elsa.Workflows.Runtime/Features/WorkflowRuntimeFeature.cs)

The reference server registers workflows from its assembly with `AddWorkflowsFrom<Program>()` in [Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

## JSON Workflows

JSON definitions are materialized by [JsonWorkflowMaterializer](../../src/modules/Elsa.Workflows.Management/Materializers/JsonWorkflowMaterializer.cs). Sample JSON workflows appear in:

- [src/apps/Elsa.Server.Web/Workflows](../../src/apps/Elsa.Server.Web/Workflows)
- [test/component/Elsa.Workflows.ComponentTests/Scenarios](../../test/component/Elsa.Workflows.ComponentTests/Scenarios)
- [test/integration/Elsa.Workflows.IntegrationTests/Scenarios](../../test/integration/Elsa.Workflows.IntegrationTests/Scenarios)

JSON workflows are important for designer compatibility and import/export tests.

## Designer Authored Workflows

The designer consumes metadata from workflow API descriptor endpoints and persists definitions through workflow definition endpoints. The server-side responsibilities are:

- expose activity descriptors
- expose expression descriptors
- expose variable descriptors
- save drafts and publish versions
- return workflow graphs and reference data

Relevant code:

- [ActivityDescriptors endpoints](../../src/modules/Elsa.Workflows.Api/Endpoints/ActivityDescriptors)
- [WorkflowDefinitions endpoints](../../src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions)
- [WorkflowDefinitionManager](../../src/modules/Elsa.Workflows.Management/Services/WorkflowDefinitionManager.cs)
- [ActivityRegistryPopulator](../../src/modules/Elsa.Workflows.Management/Services/ActivityRegistryPopulator.cs)

## ElsaScript DSL

[Elsa.Dsl.ElsaScript](../../src/modules/Elsa.Dsl.ElsaScript) is an experimental JavaScript-inspired textual DSL for authoring workflows.

The module README is the best current guide: [ElsaScript README](../../src/modules/Elsa.Dsl.ElsaScript/README.md).

Current shape:

- parser creates AST nodes from ElsaScript source
- compiler maps AST nodes to Elsa activities
- expression prefixes map into Elsa expression providers
- integration tests cover parser and compiler basics

Known limitations are documented in the README; do not assume full language coverage yet.

## Host Method Activities

Host method activities expose methods on registered host types as activities. Register host types with workflow management:

```csharp
services.AddElsa(elsa =>
{
    elsa.AddActivityHost<MyHost>();
});
```

Key files:

- [HostMethodActivity](../../src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivity.cs)
- [HostMethodActivityProvider](../../src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivityProvider.cs)
- [HostMethodActivitiesOptions](../../src/modules/Elsa.Workflows.Management/Options/HostMethodActivitiesOptions.cs)
- sample host type [Penguin](../../src/apps/Elsa.Server.Web/ActivityHosts/Penguin.cs)

Use host method activities when host application methods need to appear as designer activities without creating a full activity package.

## Workflow Definition Activities

Workflow definition activities let a workflow call another workflow definition. This is useful for composition and reuse.

Key files:

- [WorkflowDefinitionActivity](../../src/modules/Elsa.Workflows.Management/Activities/WorkflowDefinitionActivity/WorkflowDefinitionActivity.cs)
- [WorkflowDefinitionActivityDescriptorFactory](../../src/modules/Elsa.Workflows.Management/Activities/WorkflowDefinitionActivity/WorkflowDefinitionActivityDescriptorFactory.cs)
- [WorkflowDefinitionActivityProvider](../../src/modules/Elsa.Workflows.Management/Activities/WorkflowDefinitionActivity/WorkflowDefinitionActivityProvider.cs)
- [WorkflowReferenceGraphBuilder](../../src/modules/Elsa.Workflows.Management/Services/WorkflowReferenceGraphBuilder.cs)

Tests:

- [CachingAndWorkflowDefinitionActivity](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/CachingAndWorkflowDefinitionActivity)
- [WorkflowReferenceGraph](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/WorkflowReferenceGraph)

## Adding A New Activity

Typical steps:

1. Add an activity class in the owning module's `Activities` folder.
2. Derive from the appropriate base class, usually `Activity` or `CodeActivity`.
3. Define inputs and outputs with Elsa input/output models.
4. Register it with management, often via `Module.AddActivitiesFrom<TMarker>()` or `management.AddActivity<T>()`.
5. Add a unit test with `ActivityTestFixture` for activity-only behavior.
6. Add integration or component tests if it creates bookmarks, uses persistence, or participates in runtime dispatch.

Good examples:

- [WriteLine](../../src/modules/Elsa.Workflows.Core/Activities/WriteLine.cs)
- [HttpEndpoint](../../src/modules/Elsa.Http/Activities/HttpEndpoint.cs)
- [SendHttpRequest](../../src/modules/Elsa.Http/Activities/SendHttpRequest.cs)
- [RunJavaScript](../../src/modules/Elsa.Expressions.JavaScript/Activities/RunJavaScript/RunJavaScript.cs)

## Activity Metadata And UI Hints

Designer-facing metadata is produced by descriptors and UI hint handlers. Core UI hints live under [Elsa.Workflows.Core/UIHints](../../src/modules/Elsa.Workflows.Core/UIHints). Module-specific handlers live with their module, such as HTTP content type options in [Elsa.Http/UIHints](../../src/modules/Elsa.Http/UIHints).

If an activity property needs dynamic options, add an `IPropertyUIHandler` and expose it through the descriptor option endpoint.

## Authoring Choice Guide

| Need | Use |
| --- | --- |
| Compile-time workflow with strong typing | C# workflow class |
| Designer-created or imported workflow | JSON workflow definition |
| Host app method as activity | Host method activity |
| Reusable workflow composition | Workflow definition activity |
| Text DSL experiment or code-centric workflow file | ElsaScript |
| Module-specific trigger or IO | Custom activity in the owning module |
