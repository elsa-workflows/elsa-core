#!/bin/zsh
#
# PR Split Script for feat/activity-execution-call-stack
#
# Usage: ./scripts/split-pr.sh [--dry-run] [--skip-push] [--continue-from N]
#

set -e

FEATURE_BRANCH="feat/activity-execution-call-stack"
BASE_BRANCH="main"
REPO_ROOT="$(git rev-parse --show-toplevel)"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

DRY_RUN=false
SKIP_PUSH=false
CONTINUE_FROM=1
STASHED=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run) DRY_RUN=true; shift ;;
        --skip-push) SKIP_PUSH=true; shift ;;
        --continue-from) CONTINUE_FROM=$2; shift 2 ;;
        --help) head -10 "$0"; exit 0 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

run_cmd() {
    if [[ "$DRY_RUN" == "true" ]]; then
        echo -e "${YELLOW}[DRY-RUN]${NC} $1"
    else
        eval "$1"
    fi
}

verify_prerequisites() {
    log_info "Verifying prerequisites..."
    command -v gh &>/dev/null || { log_error "GitHub CLI not installed"; exit 1; }
    gh auth status &>/dev/null || { log_error "GitHub CLI not authenticated"; exit 1; }
    git rev-parse --verify "$FEATURE_BRANCH" &>/dev/null || { log_error "Branch $FEATURE_BRANCH not found"; exit 1; }
    log_success "Prerequisites verified"
}

stash_changes() {
    if [[ -n $(git status --porcelain) ]]; then
        log_warn "Stashing uncommitted changes..."
        run_cmd "git stash push -m 'split-pr.sh: auto-stash'"
        STASHED=true
    fi
}

restore_stash() {
    if [[ "$STASHED" == "true" ]]; then
        log_info "Restoring stashed changes..."
        run_cmd "git stash pop" || true
    fi
}

create_pr_branch() {
    local branch_name=$1
    local pr_title=$2
    local pr_description=$3
    shift 3
    local files=("$@")
    
    log_info "Creating branch: $branch_name"
    
    run_cmd "git checkout $BASE_BRANCH"
    run_cmd "git pull origin $BASE_BRANCH" || true
    
    git branch -D "$branch_name" 2>/dev/null || true
    run_cmd "git checkout -b $branch_name"
    
    for file in "${files[@]}"; do
        if [[ "$file" == DELETE:* ]]; then
            local filepath="${file#DELETE:}"
            if [[ -f "$REPO_ROOT/$filepath" ]]; then
                log_info "  Deleting: $filepath"
                run_cmd "git rm '$filepath'" 2>/dev/null || true
            fi
        else
            log_info "  Checkout: $file"
            if git show "$FEATURE_BRANCH:$file" &>/dev/null 2>&1; then
                run_cmd "git checkout '$FEATURE_BRANCH' -- '$file'"
            else
                log_warn "  Not found: $file"
            fi
        fi
    done
    
    if [[ -n $(git status --porcelain) ]]; then
        run_cmd "git add -A"
        run_cmd "git commit -m '$pr_title'"
        log_success "Created $branch_name"
    else
        log_warn "No changes for $branch_name"
        return
    fi
    
    if [[ "$SKIP_PUSH" != "true" ]]; then
        run_cmd "git push -u origin $branch_name --force-with-lease"
        if [[ "$DRY_RUN" != "true" ]]; then
            gh pr create --title "$pr_title" --body "$pr_description" --base "$BASE_BRANCH" --head "$branch_name" 2>/dev/null || log_warn "PR may already exist"
        fi
    fi
}

# PR 10: Meta/docs cleanup
create_pr_10() {
    log_info "========== PR 10: Cleanup — Meta/docs/CI =========="
    create_pr_branch "cleanup/meta-docs-ci" \
        "Cleanup: Remove stale meta/docs/CI artifacts" \
        "Remove stale documentation and CI artifacts." \
        "DELETE:.github/agents/release-notes.agent.md" \
        "DELETE:.github/copilot-release-notes-playbook.md" \
        "DELETE:agent-logs/http-context-loss-error-messaging.md" \
        "DELETE:design/elsa-workflows-workflow-engine-for-dotnet.png" \
        ".github/workflows/packages.yml" \
        ".github/workflows/pr.yml" \
        "README.md"
}

# PR 3: Dispatch notifications
create_pr_3() {
    log_info "========== PR 3: Cleanup — Dispatch notifications =========="
    create_pr_branch "cleanup/dispatch-notifications" \
        "Cleanup: Remove workflow dispatch notifications" \
        "Remove WorkflowDefinition/Instance Dispatching/Dispatched notifications." \
        "DELETE:src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowDefinitionDispatched.cs" \
        "DELETE:src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowDefinitionDispatching.cs" \
        "DELETE:src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowInstanceDispatched.cs" \
        "DELETE:src/modules/Elsa.Workflows.Runtime/Notifications/WorkflowInstanceDispatching.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowDispatcher.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Spy.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/TestHandler.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Tests.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/WorkflowDispatchNotifications/Workflows.cs"
}

# PR 4: Commit strategy APIs
create_pr_4() {
    log_info "========== PR 4: Cleanup — Commit strategy APIs =========="
    create_pr_branch "cleanup/commit-strategy-apis" \
        "Cleanup: Remove default commit strategy APIs" \
        "Remove SetDefaultWorkflowCommitStrategy/SetDefaultActivityCommitStrategy APIs." \
        "src/modules/Elsa.Workflows.Core/CommitStates/CommitStrategiesFeature.cs" \
        "src/modules/Elsa.Workflows.Core/CommitStates/Extensions/ModuleExtensions.cs" \
        "src/modules/Elsa.Workflows.Core/CommitStates/Options/CommitStateOptions.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/SimpleWorkflowWithoutActivityCommitStrategy.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/Tests.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultActivityCommitStrategy/WorkflowWithExplicitActivityCommitStrategy.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/SimpleWorkflowWithoutWorkflowCommitStrategy.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/Tests.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Scenarios/DefaultWorkflowCommitStrategy/WorkflowWithExplicitWorkflowCommitStrategy.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/SharedHelpers/CommitTracker.cs"
}

# PR 6: Multitenancy tasks
create_pr_6() {
    log_info "========== PR 6: Refactor — Multitenancy tasks =========="
    create_pr_branch "refactor/multitenancy-tasks" \
        "Refactor: Split TenantTaskManager into focused event handlers" \
        "Replace monolithic TenantTaskManager with focused handlers." \
        "DELETE:src/modules/Elsa.Common/Attributes/TaskDependencyAttribute.cs" \
        "DELETE:src/modules/Elsa.Common/Helpers/TopologicalTaskSorter.cs" \
        "DELETE:src/modules/Elsa.Common/Multitenancy/Contracts/ITenantScope.cs" \
        "DELETE:src/modules/Elsa.Common/Multitenancy/EventHandlers/TenantTaskManager.cs" \
        "src/modules/Elsa.Common/Multitenancy/EventHandlers/RunBackgroundTasks.cs" \
        "src/modules/Elsa.Common/Multitenancy/EventHandlers/RunStartupTasks.cs" \
        "src/modules/Elsa.Common/Multitenancy/EventHandlers/StartRecurringTasks.cs" \
        "src/modules/Elsa.Common/Multitenancy/Models/TenantScope.cs" \
        "src/modules/Elsa.Common/Features/MultitenancyFeature.cs" \
        "src/modules/Elsa.Tenants/Providers/ConfigurationTenantsProvider.cs" \
        "DELETE:test/unit/Elsa.Common.UnitTests/Helpers/TopologicalTaskSorterTests.cs" \
        "DELETE:test/unit/Elsa.Common.UnitTests/Elsa.Common.UnitTests.csproj" \
        "DELETE:test/unit/Elsa.Common.UnitTests/Codecs/ZstdTests.cs"
}

# PR 7: Infrastructure improvements
create_pr_7() {
    log_info "========== PR 7: Refactor — Infrastructure =========="
    create_pr_branch "refactor/infrastructure" \
        "Refactor: Infrastructure improvements (Mediator, Caching, misc)" \
        "Small infrastructure improvements across subsystems." \
        "src/common/Elsa.Mediator/HostedServices/BackgroundCommandSenderHostedService.cs" \
        "src/common/Elsa.Mediator/HostedServices/BackgroundEventPublisherHostedService.cs" \
        "src/common/Elsa.Mediator/Middleware/Command/Components/CommandHandlerInvokerMiddleware.cs" \
        "src/modules/Elsa.Caching/Contracts/ICacheManager.cs" \
        "src/modules/Elsa.Caching/Services/CacheManager.cs" \
        "src/modules/Elsa.Http/Services/CachingHttpWorkflowLookupService.cs" \
        "src/modules/Elsa.Workflows.Management/Features/CachingWorkflowDefinitionsFeature.cs" \
        "src/modules/Elsa.Workflows.Management/Stores/CachingWorkflowDefinitionStore.cs" \
        "src/modules/Elsa.Workflows.Runtime/Stores/CachingTriggerStore.cs" \
        "src/modules/Elsa.Common/Codecs/Zstd.cs" \
        "src/modules/Elsa.Common/Elsa.Common.csproj" \
        "src/common/Elsa.Testing.Shared/XunitLogger.cs" \
        "src/modules/Elsa.Dsl.ElsaScript/Compiler/ElsaScriptCompiler.cs" \
        "src/modules/Elsa.WorkflowProviders.BlobStorage.ElsaScript/Features/ElsaScriptBlobStorageFeature.cs"
}

# PR 2: Resilience Core
create_pr_2() {
    log_info "========== PR 2: Cleanup — Resilience Core =========="
    create_pr_branch "cleanup/resilience-core" \
        "Cleanup: Remove Resilience Core transient exception handling" \
        "Remove transient exception detection/strategy subsystem." \
        "DELETE:src/modules/Elsa.Resilience.Core/Contracts/ITransientExceptionDetector.cs" \
        "DELETE:src/modules/Elsa.Resilience.Core/Contracts/ITransientExceptionStrategy.cs" \
        "DELETE:src/modules/Elsa.Resilience.Core/Services/DefaultTransientExceptionStrategy.cs" \
        "DELETE:src/modules/Elsa.Resilience.Core/Services/TransientExceptionDetector.cs" \
        "src/modules/Elsa.Resilience/Features/ResilienceFeature.cs" \
        "src/modules/Elsa.Workflows.Runtime.Distributed/Elsa.Workflows.Runtime.Distributed.csproj" \
        "src/modules/Elsa.Workflows.Runtime.Distributed/Features/DistributedRuntimeFeature.cs" \
        "src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/DefaultTransientExceptionStrategyTests.cs" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/Elsa.Resilience.Core.UnitTests.csproj" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/ResilienceStrategyCatalogTests.cs" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/ResilienceStrategyConfigEvaluatorTests.cs" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/TestHelpers/TestDataFactory.cs" \
        "DELETE:test/unit/Elsa.Resilience.Core.UnitTests/TransientExceptionDetectorTests.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/DistributedLockResilienceTests.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedLock.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedLockProvider.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Mocks/TestDistributedSynchronizationHandle.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/DistributedLockResilience/Workflows/SimpleWorkflow.cs"
}

# PR 8: Model/API cleanups
create_pr_8() {
    log_info "========== PR 8: Refactor — Model/API cleanups =========="
    create_pr_branch "refactor/model-api-cleanups" \
        "Refactor: Minor model and API cleanups" \
        "Small refactorings to core models and APIs." \
        "src/modules/Elsa.Workflows.Core/Models/Bookmark.cs" \
        "src/modules/Elsa.Workflows.Core/Models/Output.cs" \
        "src/modules/Elsa.Workflows.Core/Attributes/ActivityAttribute.cs" \
        "src/modules/Elsa.Workflows.Core/Attributes/InputAttribute.cs" \
        "src/modules/Elsa.Workflows.Core/Attributes/OutputAttribute.cs" \
        "src/modules/Elsa.Workflows.Management/Models/TimestampFilter.cs" \
        "src/modules/Elsa.Workflows.Management/Providers/DefaultExpressionDescriptorProvider.cs" \
        "src/modules/Elsa.Workflows.Management/Extensions/WorkflowInstanceStoreExtensions.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowCancellationDispatcher.cs" \
        "DELETE:test/unit/Elsa.Workflows.Runtime.UnitTests/Services/BackgroundWorkflowCancellationDispatcherTests.cs" \
        "DELETE:test/unit/Elsa.Workflows.Runtime.UnitTests/Services/LocalWorkflowClientTests.cs" \
        "src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/GetByDefinitionId/Endpoint.cs" \
        "src/modules/Elsa.Workflows.Api/Models/LinkedWorkflowDefinitionSummary.cs" \
        "src/modules/Elsa.Workflows.Api/Services/StaticWorkflowDefinitionLinker.cs" \
        "src/clients/Elsa.Api.Client/Resources/WorkflowDefinitions/Models/WorkflowDefinitionSummary.cs" \
        "src/apps/Elsa.Server.Web/appsettings.json" \
        "test/integration/Elsa.Workflows.IntegrationTests/Scenarios/RunAsynchronousActivityOutput/Tests.cs"
}

# PR 5: HTTP bookmark
create_pr_5() {
    log_info "========== PR 5: Refactor — HTTP bookmark =========="
    create_pr_branch "refactor/http-bookmark" \
        "Refactor: HTTP activity bookmark-based error handling" \
        "Change HTTP activities to use bookmarks for missing HttpContext." \
        "src/modules/Elsa.Http/Activities/WriteFileHttpResponse.cs" \
        "src/modules/Elsa.Http/Activities/WriteHttpResponse.cs" \
        "src/modules/Elsa.Http/Extensions/BookmarkExecutionContextExtensions.cs" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Activities/HttpContextLossTests.cs" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Activities/Workflows/WriteFileHttpResponseWithoutHttpContextWorkflow.cs" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Activities/Workflows/WriteHttpResponseWithoutHttpContextWorkflow.cs" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Elsa.Http.IntegrationTests.csproj" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Helpers/NullHttpContextAccessor.cs" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/README.md" \
        "DELETE:test/integration/Elsa.Http.IntegrationTests/Usings.cs" \
        "test/unit/Elsa.Activities.UnitTests/Http/WriteFileHttpResponseTests.cs" \
        "test/unit/Elsa.Activities.UnitTests/Http/WriteHttpResponseTests.cs"
}

# PR 1: Dead code removal
create_pr_1() {
    log_info "========== PR 1: Cleanup — Dead code =========="
    create_pr_branch "cleanup/dead-code" \
        "Cleanup: Remove dead code and unused features" \
        "Remove HostMethod activities, MaterializerRegistry, and related code." \
        "DELETE:src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivity.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Activities/HostMethod/HostMethodActivityProvider.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Contracts/IHostMethodActivityDescriber.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Contracts/IHostMethodParameterValueProvider.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Contracts/IMaterializerRegistry.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Exceptions/WorkflowMaterializerNotFoundException.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Options/HostMethodActivitiesOptions.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Services/DefaultHostMethodParameterValueProvider.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Services/DelegateHostMethodParameterValueProvider.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Services/HostMethodActivityDescriber.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Services/MaterializerRegistry.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Attributes/FromServicesAttribute.cs" \
        "DELETE:src/modules/Elsa.Workflows.Management/Models/WorkflowGraphFindResult.cs" \
        "DELETE:src/modules/Elsa.Workflows.Core/Models/ActivityConstructionResult.cs" \
        "DELETE:src/modules/Elsa.Workflows.Core/Exceptions/InvalidActivityDescriptorInputException.cs" \
        "DELETE:src/modules/Elsa.Workflows.Runtime/Exceptions/WorkflowDefinitionNotFoundException.cs" \
        "DELETE:src/apps/Elsa.Server.Web/ActivityHosts/Penguin.cs" \
        "src/modules/Elsa.Workflows.Core/Models/ActivityDescriptor.cs" \
        "src/modules/Elsa.Workflows.Core/Models/ActivityConstructorContext.cs" \
        "src/modules/Elsa.Workflows.Core/Serialization/Converters/ActivityJsonConverter.cs" \
        "src/modules/Elsa.Workflows.Core/Services/ActivityDescriber.cs" \
        "src/modules/Elsa.Workflows.Core/Services/ActivityFactory.cs" \
        "src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs" \
        "src/modules/Elsa.Workflows.Management/Features/WorkflowManagementFeature.cs" \
        "src/modules/Elsa.Workflows.Management/Services/WorkflowDefinitionService.cs" \
        "src/modules/Elsa.Workflows.Management/Contracts/IWorkflowDefinitionService.cs" \
        "src/modules/Elsa.Workflows.Management/Services/CachingWorkflowDefinitionService.cs" \
        "src/modules/Elsa.Workflows.Management/Activities/WorkflowDefinitionActivity/WorkflowDefinitionActivityDescriptorFactory.cs" \
        "src/modules/Elsa.Workflows.Management/Extensions/ModuleExtensions.cs" \
        "src/modules/Elsa.Workflows.Runtime/Exceptions/WorkflowGraphNotFoundException.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs" \
        "src/apps/Elsa.Server.Web/Program.cs" \
        "test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj" \
        "test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/WorkflowServer.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/HostMethodActivities/HostMethodActivityTests.cs" \
        "DELETE:test/component/Elsa.Workflows.ComponentTests/Scenarios/HostMethodActivities/TestHostMethod.cs" \
        "DELETE:test/unit/Elsa.Workflows.Core.UnitTests/Models/JsonActivityConstructorContextHelperTests.cs" \
        "DELETE:test/unit/Elsa.Workflows.Management.UnitTests/Services/MaterializerRegistryTests.cs" \
        "DELETE:test/unit/Elsa.Workflows.Management.UnitTests/Services/CachingWorkflowDefinitionServiceTests.cs" \
        "DELETE:test/unit/Elsa.Workflows.Management.UnitTests/Services/WorkflowDefinitionServiceTests.cs" \
        "DELETE:test/unit/Elsa.Workflows.Management.UnitTests/Helpers/TestHelpers.cs" \
        "DELETE:test/unit/Elsa.Workflows.Core.UnitTests/Services/ActivityRegistryTests.cs" \
        "DELETE:test/integration/Elsa.Workflows.IntegrationTests/Core/LiteralInputTests.cs" \
        "test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Converters/ActivityJsonConverterTests.cs"
}

# PR 9: Call Stack feature
create_pr_9() {
    log_info "========== PR 9: Feature — Activity Call Stack =========="
    create_pr_branch "feat/activity-call-stack" \
        "Feature: Activity Execution Call Stack" \
        "Add call stack tracking for activity executions." \
        "src/modules/Elsa.Workflows.Core/Options/ScheduleWorkOptions.cs" \
        "src/modules/Elsa.Workflows.Core/Models/ScheduledActivityOptions.cs" \
        "src/modules/Elsa.Workflows.Core/Models/ActivityWorkItem.cs" \
        "src/modules/Elsa.Workflows.Core/Options/ActivityInvocationOptions.cs" \
        "src/modules/Elsa.Workflows.Core/Services/WorkflowExecutionContextSchedulerStrategy.cs" \
        "src/modules/Elsa.Workflows.Core/Services/ActivityExecutionContextSchedulerStrategy.cs" \
        "src/modules/Elsa.Workflows.Core/Middleware/Workflows/DefaultActivitySchedulerMiddleware.cs" \
        "src/modules/Elsa.Workflows.Core/Middleware/Activities/DefaultActivityInvokerMiddleware.cs" \
        "src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs" \
        "src/modules/Elsa.Workflows.Core/Contexts/WorkflowExecutionContext.cs" \
        "src/modules/Elsa.Workflows.Core/State/ActivityExecutionContextState.cs" \
        "src/modules/Elsa.Workflows.Core/Services/WorkflowStateExtractor.cs" \
        "src/modules/Elsa.Workflows.Core/Extensions/ActivityExecutionContextExtensions.cs" \
        "src/modules/Elsa.Workflows.Core/Extensions/WorkflowExecutionContextExtensions.cs" \
        "src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Counters.cs" \
        "src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Tokens.cs" \
        "src/modules/Elsa.Workflows.Core/Activities/Parallel.cs" \
        "src/modules/Elsa.Workflows.Core/Activities/Sequence.cs" \
        "src/modules/Elsa.Workflows.Core/Activities/While.cs" \
        "src/modules/Elsa.Workflows.Core/Behaviors/ScheduledChildCallbackBehavior.cs" \
        "src/modules/Elsa.Workflows.Core/Services/WorkflowRunner.cs" \
        "src/modules/Elsa.Workflows.Core/Options/RunWorkflowOptions.cs" \
        "src/modules/Elsa.Workflows.Runtime/Activities/ExecuteWorkflow.cs" \
        "src/modules/Elsa.Workflows.Runtime/Activities/DispatchWorkflow.cs" \
        "src/modules/Elsa.Workflows.Runtime/Messages/CreateAndRunWorkflowInstanceRequest.cs" \
        "src/modules/Elsa.Workflows.Runtime/Messages/RunWorkflowInstanceRequest.cs" \
        "src/modules/Elsa.Workflows.Runtime/Commands/DispatchWorkflowDefinitionCommand.cs" \
        "src/modules/Elsa.Workflows.Runtime/Requests/DispatchWorkflowDefinitionRequest.cs" \
        "src/modules/Elsa.Workflows.Runtime/Handlers/DispatchWorkflowRequestHandler.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/BackgroundWorkflowDispatcher.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs" \
        "src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs" \
        "src/modules/Elsa.Workflows.Runtime/Middleware/Activities/BackgroundActivityInvokerMiddleware.cs" \
        "src/modules/Elsa.Workflows.Runtime.Distributed/Features/DistributedRuntimeFeature.cs" \
        "src/modules/Elsa.Workflows.Runtime/Entities/ActivityExecutionRecord.cs" \
        "src/modules/Elsa.Workflows.Runtime/Services/DefaultActivityExecutionMapper.cs" \
        "src/modules/Elsa.Workflows.Runtime/Contracts/IActivityExecutionStore.cs" \
        "src/modules/Elsa.Workflows.Runtime/Filters/ActivityExecutionRecordFilter.cs" \
        "src/modules/Elsa.Workflows.Runtime/Stores/MemoryActivityExecutionStore.cs" \
        "src/modules/Elsa.Workflows.Runtime/Stores/NoopActivityExecutionStore.cs" \
        "src/modules/Elsa.Workflows.Runtime/Extensions/ActivityExecutionRecordExtensions.cs" \
        "src/modules/Elsa.Workflows.Runtime/Extensions/ActivityExecutionStoreExtensions.cs" \
        "src/modules/Elsa.Persistence.EFCore/Modules/Runtime/ActivityExecutionLogStore.cs" \
        "src/modules/Elsa.Persistence.EFCore/Modules/Runtime/Configurations.cs" \
        "src/modules/Elsa.Persistence.EFCore/efcore-3.7.sh" \
        "src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/20260122123013_V3_7.Designer.cs" \
        "src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/20260122123013_V3_7.cs" \
        "src/modules/Elsa.Persistence.EFCore.MySql/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs" \
        "src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/20260122123056_V3_7.Designer.cs" \
        "src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/20260122123056_V3_7.cs" \
        "src/modules/Elsa.Persistence.EFCore.Oracle/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs" \
        "src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/20260122123049_V3_7.Designer.cs" \
        "src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/20260122123049_V3_7.cs" \
        "src/modules/Elsa.Persistence.EFCore.PostgreSql/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs" \
        "src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/20260122123023_V3_7.Designer.cs" \
        "src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/20260122123023_V3_7.cs" \
        "src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs" \
        "src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/20260122123040_V3_7.Designer.cs" \
        "src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/20260122123040_V3_7.cs" \
        "src/modules/Elsa.Persistence.EFCore.Sqlite/Migrations/Runtime/RuntimeElsaDbContextModelSnapshot.cs" \
        "src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Endpoint.cs" \
        "src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Request.cs" \
        "src/modules/Elsa.Workflows.Api/Endpoints/ActivityExecutions/GetCallStack/Response.cs" \
        "src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Contracts/IActivityExecutionsApi.cs" \
        "src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Models/ActivityExecutionCallStack.cs" \
        "src/clients/Elsa.Api.Client/Resources/ActivityExecutions/Models/ActivityExecutionRecord.cs" \
        "test/unit/Elsa.Workflows.Core.UnitTests/Contexts/WorkflowExecutionContextTests.cs" \
        "test/unit/Elsa.Workflows.Core.UnitTests/Extensions/ActivityExecutionContextExtensions/ExecutionChainTests.cs" \
        "test/unit/Elsa.Workflows.Core.UnitTests/Services/WorkflowStateExtractorTests.cs" \
        "test/unit/Elsa.Workflows.Runtime.UnitTests/Extensions/ActivityExecutionStoreExtensionsTests.cs" \
        "test/unit/Elsa.Workflows.Runtime.UnitTests/Services/DefaultActivityExecutionMapperTests.cs" \
        "test/unit/Elsa.Workflows.Runtime.UnitTests/Services/DefaultWorkflowDefinitionStorePopulatorTests.cs" \
        "Elsa.sln"
}

# Main
main() {
    echo ""
    echo "=========================================="
    echo "  PR Split Script for Elsa Workflows"
    echo "=========================================="
    echo ""
    
    verify_prerequisites
    cd "$REPO_ROOT"
    
    ORIGINAL_BRANCH=$(git branch --show-current)
    stash_changes
    
    trap 'git checkout "$ORIGINAL_BRANCH" 2>/dev/null; restore_stash' EXIT
    
    log_info "Starting from PR $CONTINUE_FROM..."
    echo ""
    
    # Create PRs in merge order: 10 → 3 → 4 → 6 → 7 → 2 → 8 → 5 → 1 → 9
    [[ $CONTINUE_FROM -le 1 ]] && { create_pr_10; echo ""; }
    [[ $CONTINUE_FROM -le 2 ]] && { create_pr_3; echo ""; }
    [[ $CONTINUE_FROM -le 3 ]] && { create_pr_4; echo ""; }
    [[ $CONTINUE_FROM -le 4 ]] && { create_pr_6; echo ""; }
    [[ $CONTINUE_FROM -le 5 ]] && { create_pr_7; echo ""; }
    [[ $CONTINUE_FROM -le 6 ]] && { create_pr_2; echo ""; }
    [[ $CONTINUE_FROM -le 7 ]] && { create_pr_8; echo ""; }
    [[ $CONTINUE_FROM -le 8 ]] && { create_pr_5; echo ""; }
    [[ $CONTINUE_FROM -le 9 ]] && { create_pr_1; echo ""; }
    [[ $CONTINUE_FROM -le 10 ]] && { create_pr_9; echo ""; }
    
    git checkout "$ORIGINAL_BRANCH"
    restore_stash
    trap - EXIT
    
    echo ""
    log_success "=========================================="
    log_success "  PR Split Complete!"
    log_success "=========================================="
    echo ""
    echo "Created branches (in merge order):"
    echo "  1. cleanup/meta-docs-ci"
    echo "  2. cleanup/dispatch-notifications"
    echo "  3. cleanup/commit-strategy-apis"
    echo "  4. refactor/multitenancy-tasks"
    echo "  5. refactor/infrastructure"
    echo "  6. cleanup/resilience-core"
    echo "  7. refactor/model-api-cleanups"
    echo "  8. refactor/http-bookmark"
    echo "  9. cleanup/dead-code"
    echo " 10. feat/activity-call-stack"
    echo ""
}

main "$@"
