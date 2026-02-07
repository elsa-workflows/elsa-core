# PR Split Inventory: `feat/activity-execution-call-stack`

This document inventories all changes in the `feat/activity-execution-call-stack` branch (215 files changed, ~5,185 insertions, ~7,041 deletions vs `main`) and groups them into logical, independently reviewable PRs.

---

## PR 1: Cleanup — Remove dead code and unused features

**Rationale:** These deletions remove unused/experimental features (HostMethod activities, MaterializerRegistry, some dead exceptions, and related tests). They are independent of the call stack feature and reduce noise.

### Files

| Status | File |
|--------|------|
| D | `src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivity.cs` |
| D | `src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivityProvider.cs` |
| D | `src/modules/Elsa.Workflows.Management/Contracts/IHostMethodActivityDescriber.cs` |
| D | `src/modules/Elsa.Workflows.Management/Contracts/IHostMethodParameterValueProvider.cs` |
| D | `src/modules/Elsa.Workflows.Management/Contracts/IMaterializerRegistry.cs` |
| D | `src/modules/Elsa.Workflows.Management/Exceptions/WorkflowMaterializerNotFoundException.cs` |
| D | `src/modules/Elsa.Workflows.Management/Options/HostMethodActivitiesOptions.cs` |
| D | `src/modules/Elsa.Workflows.Management/Services/DefaultHostMethodParameterValueProvider.cs` |
| D | `src/modules/Elsa.Workflows.Management/Services/DelegateHostMethodParameterValueProvider.cs` |
| D | `src/modules/Elsa.Workflows.Management/Services/HostMethodActivityDescriber.cs` |
| D | `src/modules/Elsa.Workflows.Management/Services/MaterializerRegistry.cs` |
| D | `src/modules/Elsa.Workflows.Management/Attributes/FromServicesAttribute.cs` |
| D | `src/modules/Elsa.Workflows.Management/Models/WorkflowGraphFindResult.cs` |
| D | `src/modules/Elsa.Workflows.Core/Models/ActivityConstructionResult.cs` |
| D | `src/modules/Elsa.Workflows.Core/Exceptions/InvalidActivityDescriptorInputException.cs` |
| D | `src/modules/Elsa.Workflows.Runtime/Exceptions/WorkflowDefinitionNotFoundException.cs` |
| D | `src/apps/Elsa.Server.Web/ActivityHosts/Penguin.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/HostMethodActivities/HostMethodActivityTests.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/HostMethodActivities/TestHostMethod.cs` |
| D | `test/unit/Elsa.Workflows.Core.UnitTests/Models/JsonActivityConstructorContextHelperTests.cs` |
| D | `test/unit/Elsa.Workflows.Management.UnitTests/Services/MaterializerRegistryTests.cs` |
| D | `test/unit/Elsa.Workflows.Management.UnitTests/Services/CachingWorkflowDefinitionServiceTests.cs` |
| D | `test/unit/Elsa.Workflows.Management.UnitTests/Services/WorkflowDefinitionServiceTests.cs` |
| D | `test/unit/Elsa.Workflows.Management.UnitTests/Helpers/TestHelpers.cs` |
| D | `test/unit/Elsa.Workflows.Core.UnitTests/Services/ActivityRegistryTests.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Core/LiteralInputTests.cs` |
| M | `src/modules/Elsa.Workflows.Core/Models/ActivityDescriptor.cs` — Constructor return type changed from `ActivityConstructionResult` to `IActivity` |
| M | `src/modules/Elsa.Workflows.Core/Models/ActivityConstructorContext.cs` — Simplified to return `IActivity` directly |
| M | `src/modules/Elsa.Workflows.Core/Serialization/Converters/ActivityJsonConverter.cs` — Removed `ActivityConstructionResult` wrapper and exception logging |
| M | `src/modules/Elsa.Workflows.Core/Services/ActivityDescriber.cs` — Updated constructor lambda |
| M | `src/modules/Elsa.Workflows.Core/Services/ActivityFactory.cs` — Updated constructor call |
| M | `src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs` — Simplified `RefreshDescriptorsAsync` logic |
| M | `src/modules/Elsa.Workflows.Management/Features/WorkflowManagementFeature.cs` — Removed HostMethod registrations |
| M | `src/modules/Elsa.Workflows.Management/Services/WorkflowDefinitionService.cs` — Replaced `IMaterializerRegistry` with `Func<IEnumerable<IWorkflowMaterializer>>`, removed `TryFindWorkflowGraphAsync` |
| M | `src/modules/Elsa.Workflows.Management/Contracts/IWorkflowDefinitionService.cs` — Removed `TryFindWorkflowGraphAsync` |
| M | `src/modules/Elsa.Workflows.Management/Services/CachingWorkflowDefinitionService.cs` — Updated to match simplified interface |
| M | `src/modules/Elsa.Workflows.Management/Activities/WorkflowDefinitionActivity/WorkflowDefinitionActivityDescriptorFactory.cs` — Updated constructor lambda |
| M | `src/modules/Elsa.Workflows.Management/Extensions/ModuleExtensions.cs` — Removed HostMethod extension methods |
| M | `src/modules/Elsa.Workflows.Runtime/Exceptions/WorkflowGraphNotFoundException.cs` — Renamed/simplified |
| M | `src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs` — Use simplified API (no `TryFindWorkflowGraphAsync`) |
| M | `src/apps/Elsa.Server.Web/Program.cs` — Removed HostMethod registrations |
| M | `Elsa.sln` — Solution file changes (removed test projects) |
| M | `test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj` — Removed HostMethod references |
| M | `test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/WorkflowServer.cs` — Removed HostMethod setup |
| M | `test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Converters/ActivityJsonConverterTests.cs` — Updated for new constructor return type |

---

## PR 2: Cleanup — Remove Resilience Core transient exception handling

**Rationale:** Removes the `Elsa.Resilience.Core` transient exception detection/strategy subsystem and its dependency from the distributed runtime. Independent of other changes.

### Files

| Status | File |
|--------|------|
| D | `src/modules/Elsa.Resilience.Core/Contracts/ITransientExceptionDetector.cs` |
| D | `src/modules/Elsa.Resilience.Core/Contracts/ITransientExceptionStrategy.cs` |
| D | `src/modules/Elsa.Resilience.Core/Services/DefaultTransientExceptionStrategy.cs` |
| D | `src/modules/Elsa.Resilience.Core/Services/TransientExceptionDetector.cs` |
| M | `src/modules/Elsa.Resilience/Features/ResilienceFeature.cs` — Removed transient exception registrations |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/DefaultTransientExceptionStrategyTests.cs` |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/Elsa.Resilience.Core.UnitTests.csproj` |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/ResilienceStrategyCatalogTests.cs` |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/ResilienceStrategyConfigEvaluatorTests.cs` |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/TestHelpers/TestDataFactory.cs` |
| D | `test/unit/Elsa.Resilience.Core.UnitTests/TransientExceptionDetectorTests.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/DistributedLockResilienceTests.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedLock.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedLockProvider.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedSynchronizationHandle.cs` |
| D | `test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Workflows/SimpleWorkflow.cs` |
| M | `src/modules/Elsa.Workflows.Runtime.Distributed/Elsa.Workflows.Runtime.Distributed.csproj` — Removed Resilience/Polly references |
| M | `src/modules/Elsa.Workflows.Runtime.Distributed/Features/DistributedRuntimeFeature.cs` — Removed ResilienceFeature dependency |
| M | `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs` — Removed transient exception handling/Polly retry |
| M | `Elsa.sln` — Solution file changes |

---

## PR 3: Cleanup — Remove workflow dispatch notifications

**Rationale:** Removes the `WorkflowDefinitionDispatching/Dispatched` and `WorkflowInstanceDispatching/Dispatched` notification types and their consumers. Independent of call stack feature.

### Files

| Status | File |
|--------|------|
| D | `src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowDefinitionDispatched.cs` |
| D | `src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowDefinitionDispatching.cs` |
| D | `src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowInstanceDispatched.cs` |
| D | `src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowInstanceDispatching.cs` |
| M | `src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowDispatcher.cs` — Removed notification publishing |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Spy.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/TestHandler.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Tests.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Workflows.cs` |

---

## PR 4: Cleanup — Remove default commit strategy APIs and related tests

**Rationale:** Removes the `SetDefaultWorkflowCommitStrategy`/`SetDefaultActivityCommitStrategy` APIs and their related tests. Independent housekeeping.

### Files

| Status | File |
|--------|------|
| M | `src/modules/Elsa.Workflows.Core/CommitStates/CommitStrategiesFeature.cs` — Removed `SetDefaultWorkflowCommitStrategy`/`SetDefaultActivityCommitStrategy` methods; whitespace cleanup |
| M | `src/modules/Elsa.Workflows.Core/CommitStates/Extensions/ModuleExtensions.cs` — Removed `WithDefaultWorkflowCommitStrategy`/`WithDefaultActivityCommitStrategy` extension methods |
| M | `src/modules/Elsa.Workflows.Core/CommitStates/Options/CommitStateOptions.cs` — Removed `DefaultWorkflowCommitStrategy`/`DefaultActivityCommitStrategy` properties |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/SimpleWorkflowWithoutActivityCommitStrategy.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/Tests.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/WorkflowWithExplicitActivityCommitStrategy.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/SimpleWorkflowWithoutWorkflowCommitStrategy.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/Tests.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/WorkflowWithExplicitWorkflowCommitStrategy.cs` |
| D | `test/integration/Elsa.Workflows.IntegrationTests/SharedHelpers/CommitTracker.cs` |

---

## PR 5: Refactor — HTTP activity error handling (bookmark-based cross-boundary)

**Rationale:** Changes how `WriteHttpResponse` and `WriteFileHttpResponse` handle missing `HttpContext` — from throwing a `FaultException` to creating a bookmark for cross-boundary resume. Also removes the integration test project for HTTP context loss (replaced by unit tests). Revert C# extension member syntax back to standard extensions.

### Files

| Status | File |
|--------|------|
| M | `src/modules/Elsa.Http/Activities/WriteFileHttpResponse.cs` — Bookmark-based handling instead of `FaultException` |
| M | `src/modules/Elsa.Http/Activities/WriteHttpResponse.cs` — Bookmark-based handling instead of `FaultException` |
| M | `src/modules/Elsa.Http/Extensions/BookmarkExecutionContextExtensions.cs` — Reverted from C# extension members to standard static extension methods |
| D | `test/integration/Elsa.Http.IntegrationTests/Activities/HttpContextLossTests.cs` |
| D | `test/integration/Elsa.Http.IntegrationTests/Activities/Workflows/WriteFileHttpResponseWithoutHttpContextWorkflow.cs` |
| D | `test/integration/Elsa.Http.IntegrationTests/Activities/Workflows/WriteHttpResponseWithoutHttpContextWorkflow.cs` |
| D | `test/integration/Elsa.Http.IntegrationTests/Elsa.Http.IntegrationTests.csproj` |
| D | `test/integration/Elsa.Http.IntegrationTests/Helpers/NullHttpContextAccessor.cs` |
| D | `test/integration/Elsa.Http.IntegrationTests/README.md` |
| D | `test/integration/Elsa.Http.IntegrationTests/Usings.cs` |
| M | `test/unit/Elsa.Activities.UnitTests/Http/WriteFileHttpResponseTests.cs` — Updated tests |
| M | `test/unit/Elsa.Activities.UnitTests/Http/WriteHttpResponseTests.cs` — Updated tests |
| M | `Elsa.sln` — Removed HTTP integration test project |

---

## PR 6: Refactor — Multitenancy task management

**Rationale:** Replaces monolithic `TenantTaskManager` with smaller, focused event handlers (`RunBackgroundTasks`, `RunStartupTasks`, `StartRecurringTasks`). Removes `TopologicalTaskSorter` and `TaskDependencyAttribute`. Independent infrastructure change.

### Files

| Status | File |
|--------|------|
| D | `src/modules/Elsa.Common/Attributes/TaskDependencyAttribute.cs` |
| D | `src/modules/Elsa.Common/Helpers/TopologicalTaskSorter.cs` |
| D | `src/modules/Elsa.Common/Multitenancy/Contracts/ITenantScope.cs` |
| D | `src/modules/Elsa.Common/Multitenancy/EventHandlers/TenantTaskManager.cs` |
| A | `src/modules/Elsa.Common/Multitenancy/EventHandlers/RunBackgroundTasks.cs` |
| A | `src/modules/Elsa.Common/Multitenancy/EventHandlers/RunStartupTasks.cs` |
| A | `src/modules/Elsa.Common/Multitenancy/EventHandlers/StartRecurringTasks.cs` |
| M | `src/modules/Elsa.Common/Multitenancy/Models/TenantScope.cs` — Simplified (removed `ITenantScope` interface) |
| M | `src/modules/Elsa.Common/Features/MultitenancyFeature.cs` — Updated event handler registrations |
| M | `src/modules/Elsa.Tenants/Providers/ConfigurationTenantsProvider.cs` — Minor updates |
| D | `test/unit/Elsa.Common.UnitTests/Helpers/TopologicalTaskSorterTests.cs` |
| D | `test/unit/Elsa.Common.UnitTests/Elsa.Common.UnitTests.csproj` |
| D | `test/unit/Elsa.Common.UnitTests/Codecs/ZstdTests.cs` |

---

## PR 7: Refactor — Infrastructure improvements (Mediator, Caching, Zstd, misc)

**Rationale:** Small infrastructure improvements across several subsystems. These are low-risk, independent changes.

### Files

| Status | File |
|--------|------|
| M | `src/common/Elsa.Mediator/HostedServices/BackgroundCommandSenderHostedService.cs` — Removed redundant `try/catch` for `OperationCanceledException` |
| M | `src/common/Elsa.Mediator/HostedServices/BackgroundEventPublisherHostedService.cs` — Removed redundant `try/catch` for `OperationCanceledException` |
| M | `src/common/Elsa.Mediator/Middleware/Command/Components/CommandHandlerInvokerMiddleware.cs` — Minor cleanup |
| M | `src/modules/Elsa.Caching/Contracts/ICacheManager.cs` — Merged `FindOrCreateAsync`/`GetOrCreateAsync` into single nullable `GetOrCreateAsync` |
| M | `src/modules/Elsa.Caching/Services/CacheManager.cs` — Corresponding implementation change |
| M | `src/modules/Elsa.Http/Services/CachingHttpWorkflowLookupService.cs` — Updated to use new cache API |
| M | `src/modules/Elsa.Workflows.Management/Features/CachingWorkflowDefinitionsFeature.cs` — Updated cache usage |
| M | `src/modules/Elsa.Workflows.Management/Stores/CachingWorkflowDefinitionStore.cs` — Updated cache usage |
| M | `src/modules/Elsa.Workflows.Runtime/Stores/CachingTriggerStore.cs` — Updated cache usage |
| M | `src/modules/Elsa.Common/Codecs/Zstd.cs` — Removed `using` on non-disposable return value |
| M | `src/modules/Elsa.Common/Elsa.Common.csproj` — Minor project file changes |
| M | `src/common/Elsa.Testing.Shared/XunitLogger.cs` — Minor test helper update |
| M | `src/modules/Elsa.Dsl.ElsaScript/Compiler/ElsaScriptCompiler.cs` — Minor update |
| M | `src/modules/Elsa.WorkflowProviders.BlobStorage.ElsaScript/Features/ElsaScriptBlobStorageFeature.cs` — Minor update |

---

## PR 8: Refactor — Minor model/API cleanups

**Rationale:** Small refactorings to core models (Bookmark → record, Output default, attribute targets, etc.). Low risk, independent of call stack.

### Files

| Status | File |
|--------|------|
| M | `src/modules/Elsa.Workflows.Core/Models/Bookmark.cs` — Converted from `class` to `record` |
| M | `src/modules/Elsa.Workflows.Core/Models/Output.cs` — Changed default `MemoryBlockReference` to `Literal` |
| M | `src/modules/Elsa.Workflows.Core/Attributes/ActivityAttribute.cs` — Narrowed `AttributeUsage` targets |
| M | `src/modules/Elsa.Workflows.Core/Attributes/InputAttribute.cs` — Narrowed `AttributeUsage` targets |
| M | `src/modules/Elsa.Workflows.Core/Attributes/OutputAttribute.cs` — Narrowed `AttributeUsage` targets |
| M | `src/modules/Elsa.Workflows.Management/Models/TimestampFilter.cs` — Minor model cleanup |
| M | `src/modules/Elsa.Workflows.Management/Providers/DefaultExpressionDescriptorProvider.cs` — Minor cleanup |
| M | `src/modules/Elsa.Workflows.Management/Extensions/WorkflowInstanceStoreExtensions.cs` — Minor cleanup |
| M | `src/modules/Elsa.Workflows.Core/Extensions/WorkflowExecutionContextExtensions.cs` — Minor refactoring |
| M | `src/modules/Elsa.Workflows.Core/Extensions/ActivityExecutionContextExtensions.cs` — Refactored method call style |
| M | `src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs` — Removed Literal fallback in `TryGet`, added doc comments |
| M | `src/modules/Elsa.Workflows.Core/Contexts/WorkflowExecutionContext.cs` — Made `correlationId` parameter optional |
| M | `src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowCancellationDispatcher.cs` — Removed tenant header overhead |
| D | `test/unit/Elsa.Workflows.Runtime.UnitTests/Services/BackgroundWorkflowCancellationDispatcherTests.cs` |
| D | `test/unit/Elsa.Workflows.Runtime.UnitTests/Services/LocalWorkflowClientTests.cs` |
| M | `src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/GetByDefinitionId/Endpoint.cs` — Minor API update |
| M | `src/modules/Elsa.Workflows.Api/Models/LinkedWorkflowDefinitionSummary.cs` — Minor model update |
| M | `src/modules/Elsa.Workflows.Api/Services/StaticWorkflowDefinitionLinker.cs` — Minor update |
| M | `src/clients/Elsa.Api.Client/Resources/WorkflowDefinitions/Models/WorkflowDefinitionSummary.cs` — Minor model update |
| M | `src/apps/Elsa.Server.Web/appsettings.json` — Config changes |
| M | `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/RunAsynchronousActivityOutput/Tests.cs` — Minor test update |

---

## PR 9: Feature — Activity Execution Call Stack (core feature)

**Rationale:** This is the main feature — adding `SchedulingActivityExecutionId`, `SchedulingActivityId`, `SchedulingWorkflowInstanceId`, and `CallStackDepth` fields throughout the scheduling chain. Includes new API endpoint, EF Core migrations, and tests.

**Depends on:** PRs 1-8 should be merged first (especially PR 1 for `ActivityConstructionResult` removal, PR 2 for Resilience removal in Distributed, PR 3 for dispatch cleanup).

### Files

| Status | File |
|--------|------|
| **Core scheduling chain** | |
| M | `src/modules/Elsa.Workflows.Core/Options/ScheduleWorkOptions.cs` — Added `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Models/ScheduledActivityOptions.cs` — Added `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Models/ActivityWorkItem.cs` — Added `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Options/ActivityInvocationOptions.cs` — Added `SchedulingActivityExecutionId`, `SchedulingWorkflowInstanceId` |
| M | `src/modules/Elsa.Workflows.Core/Services/WorkflowExecutionContextSchedulerStrategy.cs` — Thread call stack through scheduling |
| M | `src/modules/Elsa.Workflows.Core/Services/ActivityExecutionContextSchedulerStrategy.cs` — Thread call stack through scheduling |
| M | `src/modules/Elsa.Workflows.Core/Middleware/Workflows/DefaultActivitySchedulerMiddleware.cs` — Thread call stack through middleware |
| M | `src/modules/Elsa.Workflows.Core/Middleware/Activities/DefaultActivityInvokerMiddleware.cs` — Thread call stack through middleware |
| **Context and state** | |
| M | `src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs` — Added `SchedulingActivityExecutionId`, `SchedulingActivityId`, `SchedulingWorkflowInstanceId`, `CallStackDepth` properties |
| M | `src/modules/Elsa.Workflows.Core/Contexts/WorkflowExecutionContext.cs` — Populate call stack fields in `CreateActivityExecutionContextAsync` |
| M | `src/modules/Elsa.Workflows.Core/State/ActivityExecutionContextState.cs` — Added `CallStackDepth` to state |
| M | `src/modules/Elsa.Workflows.Core/Services/WorkflowStateExtractor.cs` — Persist/restore `CallStackDepth` |
| M | `src/modules/Elsa.Workflows.Core/Extensions/ActivityExecutionContextExtensions.cs` — Added `GetExecutionChain()` method |
| **Composite activities** | |
| M | `src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Counters.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Tokens.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Activities/Parallel.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Activities/Sequence.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Activities/While.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Behaviors/ScheduledChildCallbackBehavior.cs` — Pass `SchedulingActivityExecutionId` |
| M | `src/modules/Elsa.Workflows.Core/Services/WorkflowRunner.cs` — Pass call stack through workflow start |
| **Cross-workflow linkage** | |
| M | `src/modules/Elsa.Workflows.Core/Options/RunWorkflowOptions.cs` — Added `SchedulingActivityExecutionId`, `SchedulingWorkflowInstanceId` |
| M | `src/modules/Elsa.Workflows.Runtime/Activities/ExecuteWorkflow.cs` — Pass call stack to child workflow |
| M | `src/modules/Elsa.Workflows.Runtime/Activities/DispatchWorkflow.cs` — Pass call stack to dispatched workflow |
| M | `src/modules/Elsa.Workflows.Runtime/Messages/CreateAndRunWorkflowInstanceRequest.cs` — Added call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime/Messages/RunWorkflowInstanceRequest.cs` — Added call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime/Commands/DispatchWorkflowDefinitionCommand.cs` — Added call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime/Requests/DispatchWorkflowDefinitionRequest.cs` — Added call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime/Handlers/DispatchWorkflowRequestHandler.cs` — Thread call stack through dispatch |
| M | `src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowDispatcher.cs` — Thread call stack through dispatch |
| M | `src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs` — Thread call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs` — Thread call stack (also has resilience removal from PR 2) |
| M | `src/modules/Elsa.Workflows.Runtime/Middleware/Activities/BackgroundActivityInvokerMiddleware.cs` — Thread call stack |
| M | `src/modules/Elsa.Workflows.Runtime.Distributed/Features/DistributedRuntimeFeature.cs` — Updated DI |
| **Persistence — Entity and store** | |
| M | `src/modules/Elsa.Workflows.Runtime/Entities/ActivityExecutionRecord.cs` — Added `SchedulingActivityExecutionId`, `SchedulingActivityId`, `SchedulingWorkflowInstanceId`, `CallStackDepth` |
| M | `src/modules/Elsa.Workflows.Runtime/Services/DefaultActivityExecutionMapper.cs` — Map call stack fields |
| M | `src/modules/Elsa.Workflows.Runtime/Contracts/IActivityExecutionStore.cs` — New query method |
| M | `src/modules/Elsa.Workflows.Runtime/Filters/ActivityExecutionRecordFilter.cs` — New filter field |
| M | `src/modules/Elsa.Workflows.Runtime/Stores/MemoryActivityExecutionStore.cs` — Implement new query |
| M | `src/modules/Elsa.Workflows.Runtime/Stores/NoopActivityExecutionStore.cs` — Implement new query |
| A | `src/modules/Elsa.Workflows.Runtime/Extensions/ActivityExecutionRecordExtensions.cs` — New extension methods |
| A | `src/modules/Elsa.Workflows.Runtime/Extensions/ActivityExecutionStoreExtensions.cs` — New extension methods |
| M | `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/ActivityExecutionLogStore.cs` — Updated EF store |
| M | `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/Configurations.cs` — New column configurations |
| A | `src/modules/Elsa.Persistence.EFCore/efcore-3.7.sh` — Migration generation script |
| **EF Core Migrations (V3_7)** | |
| A | `src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/20260122123013_V3_7.Designer.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/20260122123013_V3_7.cs` |
| M | `src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/20260122123056_V3_7.Designer.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/20260122123056_V3_7.cs` |
| M | `src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/20260122123049_V3_7.Designer.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/20260122123049_V3_7.cs` |
| M | `src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/20260122123023_V3_7.Designer.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/20260122123023_V3_7.cs` |
| M | `src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/20260122123040_V3_7.Designer.cs` |
| A | `src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/20260122123040_V3_7.cs` |
| M | `src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs` |
| **API endpoint** | |
| A | `src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Endpoint.cs` |
| A | `src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Request.cs` |
| A | `src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Response.cs` |
| M | `src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Contracts/IActivityExecutionsApi.cs` — Added GetCallStack |
| A | `src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Models/ActivityExecutionCallStack.cs` — New model |
| M | `src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Models/ActivityExecutionRecord.cs` — Added call stack fields |
| **Tests** | |
| A | `test/unit/Elsa.Workflows.Core.UnitTests/Contexts/WorkflowExecutionContextTests.cs` |
| A | `test/unit/Elsa.Workflows.Core.UnitTests/Extensions/ActivityExecutionContextExtensions/ExecutionChainTests.cs` |
| A | `test/unit/Elsa.Workflows.Core.UnitTests/Services/WorkflowStateExtractorTests.cs` |
| A | `test/unit/Elsa.Workflows.Runtime.UnitTests/Extensions/ActivityExecutionStoreExtensionsTests.cs` |
| A | `test/unit/Elsa.Workflows.Runtime.UnitTests/Services/DefaultActivityExecutionMapperTests.cs` |
| M | `test/unit/Elsa.Workflows.Runtime.UnitTests/Services/DefaultWorkflowDefinitionStorePopulatorTests.cs` |

---

## PR 10: Cleanup — Remove meta/docs/CI artifacts

**Rationale:** Remove stale documentation, agent files, and minor CI/README updates that were part of the development process.

### Files

| Status | File |
|--------|------|
| D | `.github/agents/release-notes.agent.md` |
| D | `.github/copilot-release-notes-playbook.md` |
| M | `.github/workflows/packages.yml` — Minor CI update |
| M | `.github/workflows/pr.yml` — Minor CI update |
| M | `README.md` — Minor update |
| D | `agent-logs/http-context-loss-error-messaging.md` |
| D | `design/elsa-workflows-workflow-engine-for-dotnet.png` |
| A | `plan-activityExecutionCallStack.prompt.md` — Implementation plan (consider whether to keep) |

---

## Shared Files (appear in multiple PRs)

Some files contain changes that span multiple logical groups. When splitting into actual PRs, these files will need to be carefully partitioned:

| File | PRs | Notes |
|------|-----|-------|
| `Elsa.sln` | 1, 2, 5, 6 | Each PR removes/adds different test projects |
| `src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs` | 8, 9 | PR 8: doc comments + `TryGet` Literal removal; PR 9: call stack fields |
| `src/modules/Elsa.Workflows.Core/Contexts/WorkflowExecutionContext.cs` | 8, 9 | PR 8: optional param; PR 9: call stack population |
| `src/modules/Elsa.Workflows.Core/Extensions/ActivityExecutionContextExtensions.cs` | 8, 9 | PR 8: method call style refactor; PR 9: `GetExecutionChain()` |
| `src/modules/Elsa.Workflows.Runtime.Distributed/Features/DistributedRuntimeFeature.cs` | 2, 9 | PR 2: remove Resilience dependency; PR 9: call stack threading |
| `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs` | 2, 9 | PR 2: remove Polly/transient retry; PR 9: call stack fields |
| `src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowDispatcher.cs` | 3, 9 | PR 3: remove notifications; PR 9: add call stack fields |
| `src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs` | 1, 9 | PR 1: simplified graph lookup; PR 9: call stack fields |

## Suggested Merge Order

The PRs can be merged in the following order to minimize conflicts:

1. **PR 10** (Meta/docs cleanup) — No code dependencies
2. **PR 3** (Remove dispatch notifications) — Standalone deletion
3. **PR 4** (Remove default commit strategy APIs) — Standalone deletion
4. **PR 6** (Multitenancy refactor) — Standalone refactor
5. **PR 7** (Infrastructure: Mediator, Caching, Zstd) — Standalone improvements
6. **PR 2** (Remove Resilience Core) — Standalone deletion
7. **PR 8** (Minor model/API cleanups) — Small refactors
8. **PR 5** (HTTP bookmark refactor) — Standalone HTTP behavior change
9. **PR 1** (Dead code removal: HostMethod, Materializer, etc.) — Larger cleanup that touches many modules
10. **PR 9** (Activity Execution Call Stack feature) — The main feature, depends on clean code from previous PRs

---

## Summary

| PR | Category | Files Changed | Risk |
|----|----------|---------------|------|
| 1 | Dead code removal | ~35 | Medium |
| 2 | Resilience removal | ~19 | Medium |
| 3 | Dispatch notifications removal | ~9 | Low |
| 4 | Commit strategy cleanup | ~10 | Low |
| 5 | HTTP bookmark refactor | ~13 | Medium |
| 6 | Multitenancy refactor | ~12 | Medium |
| 7 | Infrastructure improvements | ~14 | Low |
| 8 | Model/API cleanups | ~19 | Low-Medium |
| 9 | Call Stack feature (core) | ~60 | High |
| 10 | Meta/docs cleanup | ~8 | None |
| **Total** | | **~215** | |
