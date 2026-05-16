# HTTP, Scheduling, And Resilience

HTTP, scheduling, and resilience are extension modules layered on top of workflow core, management, runtime, and expressions.

## HTTP Module

Start in [src/modules/Elsa.Http](../../src/modules/Elsa.Http).

[HttpFeature](../../src/modules/Elsa.Http/Features/HttpFeature.cs) owns:

- inbound HTTP endpoint workflows
- outbound HTTP request activities
- route matching and route table updates
- HTTP bookmark payloads
- HTTP request/response content parsing and writing
- downloadable content handling
- file cache and zip management
- correlation and workflow instance ID selectors
- HTTP activity descriptors and UI hints
- HTTP resilience strategy registration
- HTTP ingress source registration for graceful shutdown

The public module extension is [UseHttp](../../src/modules/Elsa.Http/Extensions/ModuleExtensions.cs).

## Inbound HTTP Workflows

The inbound path is:

```mermaid
flowchart LR
    Request["ASP.NET request"] --> Middleware["HttpWorkflowsMiddleware"]
    Middleware --> RouteTable["IRouteTable / IRouteMatcher"]
    RouteTable --> Lookup["IHttpWorkflowLookupService"]
    Lookup --> Runtime["Workflow runtime"]
    Runtime --> Activity["HttpEndpoint activity"]
    Activity --> Response["HTTP response activity"]
```

Important files:

- [HttpWorkflowsMiddleware](../../src/modules/Elsa.Http/Middleware/HttpWorkflowsMiddleware.cs)
- [HttpEndpoint](../../src/modules/Elsa.Http/Activities/HttpEndpoint.cs)
- [HttpEndpointBase](../../src/modules/Elsa.Http/Activities/HttpEndpointBase.cs)
- [RouteMatcher](../../src/modules/Elsa.Http/Services/RouteMatcher.cs)
- [RouteTable](../../src/modules/Elsa.Http/Services/RouteTable.cs)
- [DefaultRouteTableUpdater](../../src/modules/Elsa.Http/Services/DefaultRouteTableUpdater.cs)
- [HttpWorkflowLookupService](../../src/modules/Elsa.Http/Services/HttpWorkflowLookupService.cs)

Hosts enable middleware with [UseWorkflows](../../src/modules/Elsa.Http/Extensions/ApplicationBuilderExtensions.cs).

## Outbound HTTP

Outbound HTTP activities:

- [SendHttpRequest](../../src/modules/Elsa.Http/Activities/SendHttpRequest.cs)
- [FlowSendHttpRequest](../../src/modules/Elsa.Http/Activities/FlowSendHttpRequest.cs)
- [DownloadHttpFile](../../src/modules/Elsa.Http/Activities/DownloadHttpFile.cs)

Supporting services include `HttpClientFileDownloader`, content factories, content parsers, and downloadable content handlers.

## HTTP Security And Faults

HTTP endpoint authorization and faults are configurable through `HttpFeature`:

- [AuthenticationBasedHttpEndpointAuthorizationHandler](../../src/modules/Elsa.Http/Handlers/AuthenticationBasedHttpEndpointAuthorizationHandler.cs)
- [AllowAnonymousHttpEndpointAuthorizationHandler](../../src/modules/Elsa.Http/Handlers/AllowAnonymousHttpEndpointAuthorizationHandler.cs)
- [DefaultHttpEndpointFaultHandler](../../src/modules/Elsa.Http/Handlers/DefaultHttpEndpointFaultHandler.cs)
- [DetailedHttpEndpointFaultHandler](../../src/modules/Elsa.Http/Handlers/DetailedHttpEndpointFaultHandler.cs)

The module registers authorization services because the authentication-based handler requires them.

## HTTP Tests

Good test entry points:

- [test/unit/Elsa.Http.UnitTests](../../test/unit/Elsa.Http.UnitTests)
- [test/integration/Elsa.Http.IntegrationTests](../../test/integration/Elsa.Http.IntegrationTests)
- [test/component/Elsa.Workflows.ComponentTests/Scenarios/Activities/Http](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/Activities/Http)
- [test/component/Elsa.Workflows.ComponentTests/Scenarios/HttpWorkflows](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/HttpWorkflows)

## Scheduling Module

Start in [src/modules/Elsa.Scheduling](../../src/modules/Elsa.Scheduling).

[SchedulingFeature](../../src/modules/Elsa.Scheduling/Features/SchedulingFeature.cs) registers:

- local scheduler
- cron parser
- trigger scheduler
- bookmark scheduler
- workflow scheduler
- tenant schedule updater
- create-schedules background task
- `ScheduleWorkflows` handlers
- Cron trigger payload validator
- scheduled-trigger ingress source for graceful shutdown
- scheduling activities through workflow management

The public extension is [UseScheduling](../../src/modules/Elsa.Scheduling/Extensions/ModuleExtensions.cs).

## Scheduling Concepts

Scheduled workflows typically create trigger or bookmark payloads that the scheduler can wake later. Important files:

- [Bookmarks](../../src/modules/Elsa.Scheduling/Bookmarks)
- [Services](../../src/modules/Elsa.Scheduling/Services)
- [Handlers](../../src/modules/Elsa.Scheduling/Handlers)
- [HostedServices](../../src/modules/Elsa.Scheduling/HostedServices)
- [TriggerPayloadValidators](../../src/modules/Elsa.Scheduling/TriggerPayloadValidators)

The scheduler integrates with tenancy by reacting to tenant activation/deletion events.

## Resilience Module

Start in [src/modules/Elsa.Resilience](../../src/modules/Elsa.Resilience) and [Elsa.Resilience.Core](../../src/modules/Elsa.Resilience.Core).

[ResilienceFeature](../../src/modules/Elsa.Resilience/Features/ResilienceFeature.cs) registers:

- activity descriptor modifier for resilient activities
- resilience strategy catalog
- strategy config evaluator
- resilient activity invoker
- configuration strategy source
- retry attempt recorders/readers
- transient exception detector and strategy
- FastEndpoints assembly for resilience descriptors/testing endpoints

HTTP registers [HttpResilienceStrategy](../../src/modules/Elsa.Http/Resilience/HttpResilienceStrategy.cs) with resilience in `HttpFeature.Configure()`.

## Resilience Concepts

Core contracts:

- [IResilienceStrategy](../../src/modules/Elsa.Resilience.Core/Contracts/IResilienceStrategy.cs)
- [IResilientActivity](../../src/modules/Elsa.Resilience.Core/Contracts/IResilientActivity.cs)
- [IResilientActivityInvoker](../../src/modules/Elsa.Resilience.Core/Contracts/IResilientActivityInvoker.cs)
- [IRetryAttemptRecorder](../../src/modules/Elsa.Resilience.Core/Contracts/IRetryAttemptRecorder.cs)
- [ITransientExceptionDetector](../../src/modules/Elsa.Resilience.Core/Contracts/ITransientExceptionDetector.cs)

Use resilience when an activity performs IO that can fail transiently. Keep strategy types registered by the owning module.

## Cross-Cutting Graceful Shutdown

HTTP and Scheduling both register runtime ingress sources so graceful shutdown can pause new external work. When adding a new external event source, implement and register an `IIngressSource` in the owning module, then add runtime tests that prove pause/resume/drain behavior.
