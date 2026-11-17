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

The default route prefix is `elsa/api`, defined in [ApiEndpointOptions](../../src/modules/Elsa.Workflows.Api/Options/ApiEndpointOptions.cs). Endpoint-routed ASP.NET hosts apply it with [MapWorkflowsApi](../../src/common/Elsa.Api.Common/Extensions/WebApplicationExtensions.cs):

```csharp
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.MapWorkflowsApi(routePrefix);
```

With the default prefix, endpoint paths look like `/elsa/api/workflow-definitions`.

When saving or publishing with `POST /elsa/api/workflow-definitions`, supply a nonblank `model.name`.
With endpoint security enabled, saving a draft requires `workflows/definitions:write`.
A save request with `publish: true` also requires `workflows/definitions:publish`.
If the caller lacks publish permission, the request returns HTTP 403 before creating or changing a draft.
The server trims surrounding whitespace. A missing, null, empty, or whitespace-only name returns HTTP 400 with a validation error under `errors.name`; the definition is not saved or published.

For multipart imports with `POST /elsa/api/workflow-definitions/import-files`, uploads with
`Content-Type: application/json` are read as definitions; other uploads are read as ZIP
archives, selecting entries with a case-insensitive `.json` suffix. Malformed JSON,
JSON `null`, or unreadable ZIP data returns HTTP 400 with a standard validation error
under `errors.generalErrors`. The message identifies the upload (and the entry for
malformed JSON inside a ZIP), without parser or exception details. If any selected
file cannot be read, no definitions from that request are imported.

New HTTP workflow starts use the configured workflow activation strategy and reserve the instance ID before running activities. A request that cannot start due to activation or an occupied ID receives HTTP 409. A bookmark request still resumes its selected instance. Definition dispatchers reject duplicate instance creation too. An asynchronous dispatcher may accept the request before its worker detects an occupied ID.

A successfully prepared start has a stored initial execution snapshot. If execution stops before its first commit, the runtime can use its interrupted execution recovery policy. An HTTP caller should use a fresh instance ID for an intentional new execution.

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

Endpoint classes typically derive from Elsa API base classes in [Elsa.Api.Common](../../src/common/Elsa.Api.Common). They configure route, verb, permissions, and response shape in `Configure()`, then implement either `ExecuteAsync` or `HandleAsync` depending on the FastEndpoints pattern used by that area of the module.

When adding endpoints:

- keep one endpoint per folder/action
- keep request and response models near the endpoint
- follow the `ExecuteAsync` or `HandleAsync` pattern used by nearby endpoints in the same area
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

`WorkflowInstanceHub` carries `[Authorize]`, so every connection requires authentication. The `ObserveInstanceAsync` method additionally enforces `read:workflow-instances` (or wildcard `read:*` or `*`) and tenant access: callers can only observe workflow instances that belong to their current tenant or are tenant-agnostic.

## Authorization And Read-Only Mode

Workflow API registers [NotReadOnlyRequirementHandler](../../src/modules/Elsa.Workflows.Api/Requirements/NotReadOnlyRequirement.cs) and the policy name in [AuthorizationPolicies](../../src/modules/Elsa.Workflows.Api/Constants/AuthorizationPolicies.cs). Mutable workflow definition endpoints use this policy to honor management read-only mode.

Bulk definition deletion, version deletion, publishing and retraction check the stored definitions before starting the operation.
If a batch would change a read-only or system definition, it returns HTTP 403 before changing any definition in the batch.
Version deletion and revert also check any version whose latest status the operation would change.
Publishing checks the version it would unpublish. Automatic reference updates skip protected consumers.

Identity and API key setup are supplied by `Elsa.Identity`; see [Identity, Tenancy, And Security](identity-tenancy-security.md).

## API Client

The generated or hand-maintained client project is [src/clients/Elsa.Api.Client](../../src/clients/Elsa.Api.Client). When endpoint contracts or enums change, check whether the client has mirrored models that need updating. The graceful shutdown plan explicitly called out mirroring `WorkflowSubStatus.Interrupted` in the API client; `WorkflowSubStatus.Cancelling` was subsequently added and should also be kept in sync — see [Workflow Runtime](workflow-runtime.md#cancellation-and-the-cancelling-sub-status).

## JSON Serialization Errors

Hosts can use [UseJsonSerializationErrorHandler](../../src/modules/Elsa.Workflows.Api/Extensions/ApplicationBuilderExtensions.cs), which installs middleware that returns JSON error responses for serialization failures. The reference server maps it after workflow API endpoints.

## HTTP workflow authorization

Turn on **Authorize** in the HTTP Endpoint activity to require authentication and enforce its optional **Policy**.
With **Authorize** off, **Policy** is ignored for starts and resumes, including in workflow definitions that already
set only a policy. Enable **Authorize** explicitly when updating those definitions.

## Inbound workflow headers

The HTTP Endpoint activity omits authentication, cookie, API-key, token, secret, and signature headers from its
serializable `Headers` output. Activities running in the original HTTP request can read credential headers from the
live `HttpRequest`. Keep values from the live request out of persistent workflow variables, logs and responses.
Review access to execution records created before this change and rotate credentials confirmed to have been retained.

## Resuming a multi-file response

The HTTP File Response activity caches a ZIP only when resumability is enabled, the caller has a stable
authenticated subject, and the workflow sets `DownloadCorrelationId` to a server-selected key for the exact
download and content revision. Retry with the same key and caller to reuse the existing ZIP.
Anonymous requests and workflows without that key still generate a ZIP on demand.
Client-supplied `x-download-id` values and old cache entries do not select a ZIP.

## Downloading a remote file in an HTTP response

The HTTP File Response activity treats strings as literal file content. For an intentional remote fetch, pass a typed
`Uri` and add the exact DNS hostname to `HttpActivityOptions.RemoteDownloadHosts` in the host configuration.
The built-in downloader uses direct HTTPS on the default port, verifies the resolved destination at connection time,
and rejects redirects. Configure the final URL when a download service normally redirects clients.

## Authenticated webhook delivery

Before enabling inbound delivery, configure a distinct base64-encoded 32-byte signing key for each existing source
ID through `WebhookIngressOptions.SigningKeys`. The legacy unsigned request format is rejected. Send the following
headers with a valid webhook event JSON body of at most 1 MiB.

- `X-Elsa-Webhook-Source` carries the configured source ID.
- `X-Elsa-Webhook-Delivery` carries a unique delivery ID that remains the same across retries.
- `X-Elsa-Webhook-Timestamp` carries Unix seconds within five minutes of the collector's clock.
- `X-Elsa-Webhook-Signature` carries the base64 HMAC-SHA256 over the source ID, delivery ID and timestamp with an LF
  after each field, followed immediately by the exact body bytes.

Source and delivery IDs may contain ASCII letters, digits, dots, underscores and hyphens. A source may deliver only
its configured event types. The server claims the delivery before dispatch and returns HTTP 409 for an already
claimed ID or if it cannot reserve the ID. The default replay store is bounded to one process and loses claims on
restart. Multi-replica or restart-resilient deployments must register a shared, atomic `IWebhookReplayStore` before
enabling sender traffic. Keep source keys in the secret store and share them only with the corresponding sender or
authorized signing ingress.

## Endpoint Discovery Command

To quickly list routes:

```bash
rg "Get\\(|Post\\(|Delete\\(|Put\\(|Patch\\(|Routes\\(|Verbs\\(" src/modules/Elsa.Workflows.Api/Endpoints src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints -g "Endpoint.cs"
```
