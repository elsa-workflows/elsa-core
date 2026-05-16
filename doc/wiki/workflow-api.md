# Workflow API

The workflow API exposes management, runtime, descriptors, execution logs, tasks, installed features, runtime admin, and real-time workflow updates. It uses FastEndpoints with Elsa-specific serializer configuration.

Start in [src/modules/Elsa.Workflows.Api](../../src/modules/Elsa.Workflows.Api) and shared API infrastructure in [src/common/Elsa.Api.Common](../../src/common/Elsa.Api.Common).

## Feature Wiring

[WorkflowsApiFeature](../../src/modules/Elsa.Workflows.Api/Features/WorkflowsApiFeature.cs):

- depends on workflow instances, workflow management, workflow runtime, and SAS tokens
- registers its endpoint assembly with the module
- calls `AddFastEndpointsFromModule()`
- configures API serialization
- registers `IWorkflowDefinitionLinker`
- registers read-only-mode authorization requirement handling
- registers workflow instance export naming

The module extension is [UseWorkflowsApi](../../src/modules/Elsa.Workflows.Api/Extensions/ModuleExtensions.cs).

## Route Prefix

The default route prefix is `elsa/api`, defined in [ApiEndpointOptions](../../src/modules/Elsa.Workflows.Api/Options/ApiEndpointOptions.cs). ASP.NET hosts apply it with [UseWorkflowsApi](../../src/common/Elsa.Api.Common/Extensions/WebApplicationExtensions.cs):

```csharp
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.UseWorkflowsApi(routePrefix);
```

With the default prefix, endpoint paths look like `/elsa/api/workflow-definitions`.

## FastEndpoints Registration

FastEndpoints assemblies are collected through module properties:

- [AddFastEndpointsAssembly](../../src/common/Elsa.Api.Common/Extensions/ModuleExtensions.cs)
- [AddFastEndpointsFromModule](../../src/common/Elsa.Api.Common/Extensions/ModuleExtensions.cs)

This allows multiple features to contribute endpoints before FastEndpoints is registered.

## Endpoint Categories

| Category | Folder | Examples |
| --- | --- | --- |
| Workflow definitions | [Endpoints/WorkflowDefinitions](../../src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions) | list, get, post, publish, retract, delete, import, export, dispatch, execute, graph, refresh, reload. |
| Workflow instances | [Endpoints/WorkflowInstances](../../src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances) | list, get, delete, cancel, bulk cancel/delete, import/export, execution state, variables, journal. |
| Activity executions | [Endpoints/ActivityExecutions](../../src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions) and [ActivityExecutionSummaries](../../src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutionSummaries) | list, get, count, report, call stack, summaries. |
| Descriptors | [ActivityDescriptors](../../src/modules/Elsa.Workflows.Api/Endpoints/ActivityDescriptors), [VariableTypes](../../src/modules/Elsa.Workflows.Api/Endpoints/VariableTypes), [StorageDrivers](../../src/modules/Elsa.Workflows.Api/Endpoints/StorageDrivers), [IncidentStrategies](../../src/modules/Elsa.Workflows.Api/Endpoints/IncidentStrategies), [CommitStrategies](../../src/modules/Elsa.Workflows.Api/Endpoints/CommitStrategies), [Scripting](../../src/modules/Elsa.Workflows.Api/Endpoints/Scripting) | designer metadata and option providers. |
| Runtime admin | [Endpoints/RuntimeAdmin](../../src/modules/Elsa.Workflows.Api/Endpoints/RuntimeAdmin) | status, pause, resume, force drain. |
| Events and tasks | [Endpoints/Events](../../src/modules/Elsa.Workflows.Api/Endpoints/Events), [Endpoints/Tasks](../../src/modules/Elsa.Workflows.Api/Endpoints/Tasks) | trigger event, complete task. |
| Package and features | [Endpoints/Package](../../src/modules/Elsa.Workflows.Api/Endpoints/Package), [Endpoints/Features](../../src/modules/Elsa.Workflows.Api/Endpoints/Features) | package version and installed feature metadata. |

## Common Endpoint Shape

Endpoint classes typically derive from Elsa API base classes in [Elsa.Api.Common](../../src/common/Elsa.Api.Common). They configure route, verb, permissions, and response shape in `Configure()`, then implement `ExecuteAsync`.

When adding endpoints:

- keep one endpoint per folder/action
- keep request and response models near the endpoint
- use `ConfigurePermissions` for protected operations
- use the configured API serializer rather than custom JSON settings
- add route examples to relevant docs when behavior is externally visible

## Real-Time Workflow Updates

Real-time updates live under [RealTime](../../src/modules/Elsa.Workflows.Api/RealTime):

- [WorkflowInstanceHub](../../src/modules/Elsa.Workflows.Api/RealTime/Hubs/WorkflowInstanceHub.cs)
- [BroadcastWorkflowProgress](../../src/modules/Elsa.Workflows.Api/RealTime/Handlers/BroadcastWorkflowProgress.cs)
- client contract [IWorkflowInstanceClient](../../src/modules/Elsa.Workflows.Api/RealTime/Contracts/IWorkflowInstanceClient.cs)
- messages for activity and workflow execution updates

Hosts map these hubs with `app.UseWorkflowsSignalRHubs()` when SignalR is enabled.

## Authorization And Read-Only Mode

Workflow API registers [NotReadOnlyRequirementHandler](../../src/modules/Elsa.Workflows.Api/Requirements/NotReadOnlyRequirement.cs) and the policy name in [AuthorizationPolicies](../../src/modules/Elsa.Workflows.Api/Constants/AuthorizationPolicies.cs). Mutable workflow definition endpoints use this policy to honor management read-only mode.

Identity and API key setup are supplied by `Elsa.Identity`; see [Identity, Tenancy, And Security](identity-tenancy-security.md).

## API Client

The generated or hand-maintained client project is [src/clients/Elsa.Api.Client](../../src/clients/Elsa.Api.Client). When endpoint contracts or enums change, check whether the client has mirrored models that need updating. The graceful shutdown plan explicitly called out mirroring `WorkflowSubStatus.Interrupted` in the API client.

## JSON Serialization Errors

Hosts can use [UseJsonSerializationErrorHandler](../../src/modules/Elsa.Workflows.Api/Extensions/ApplicationBuilderExtensions.cs), which installs middleware that returns JSON error responses for serialization failures. The reference server maps it after workflow API endpoints.

## Endpoint Discovery Command

To quickly list routes:

```bash
rg "Get\\(|Post\\(|Delete\\(|Put\\(|Patch\\(" src/modules/Elsa.Workflows.Api/Endpoints src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints -g "Endpoint.cs"
```
