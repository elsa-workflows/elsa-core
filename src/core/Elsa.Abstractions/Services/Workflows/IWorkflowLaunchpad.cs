using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Collects and creates workflow instances from triggers and bookmarks to execute.
    /// </summary>
    public interface IWorkflowLaunchpad
    {
        /// <summary>
        /// Collects and creates workflow instances that are ready for execution. This takes into account both resumable (suspended) workflows as well as startable workflows.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> FindWorkflowsAsync(WorkflowsQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects and creates workflow instances for startable workflows.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<StartableWorkflow>> FindStartableWorkflowsAsync(WorkflowsQuery query, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new workflow instance with execution pending for the specified workflow blueprint using the specified starting activity ID.
        /// </summary>
        Task<StartableWorkflow?> FindStartableWorkflowAsync(string workflowDefinitionId, string? activityId = default, string? correlationId = default, string? contextId = default, string? tenantId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new workflow instance with execution pending for the specified workflow blueprint version using the specified starting activity ID.
        /// </summary>
        Task<StartableWorkflow?> FindStartableWorkflowAsync(string workflowDefinitionId, int version, string? activityId = default, string? correlationId = default, string? contextId = default, string? tenantId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new workflow instance with execution pending for the specified workflow blueprint using the specified starting activity ID.
        /// </summary>
        Task<StartableWorkflow?> FindStartableWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string? activityId = default, string? correlationId = default, string? contextId = default, string? tenantId = default, bool throwIfRunningAndSingleton = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects and executes the specified startable workflow.
        /// </summary>
        Task FindAndExecuteStartableWorkflowAsync(string workflowDefinitionId, string? activityId = default, string? correlationId = default, string? contextId = default, WorkflowInput? input = default, string? tenantId = default, bool throwIfRunningAndSingleton = false, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Collects and executes the specified startable workflow.
        /// </summary>
        Task<RunWorkflowResult> FindAndExecuteStartableWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string? activityId = default, string? correlationId = default, string? contextId = default, WorkflowInput? input = default, bool throwIfRunningAndSingleton = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a list of pending workflows.
        /// </summary>
        Task ExecutePendingWorkflowsAsync(IEnumerable<CollectedWorkflow> pendingWorkflows, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a pending workflow.
        /// </summary>
        Task<RunWorkflowResult> ExecutePendingWorkflowAsync(CollectedWorkflow collectedWorkflow, WorkflowInput? input = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a pending workflow.
        /// </summary>
        Task<RunWorkflowResult> ExecutePendingWorkflowAsync(string workflowInstanceId, string? activityId = default, WorkflowInput? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a list of pending workflows for execution.
        /// </summary>
        Task DispatchPendingWorkflowsAsync(IEnumerable<CollectedWorkflow> pendingWorkflows, WorkflowInput? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a pending workflow for execution.
        /// </summary>
        Task DispatchPendingWorkflowAsync(CollectedWorkflow collectedWorkflow, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Dispatches a pending workflow for execution.
        /// </summary>
        Task DispatchPendingWorkflowAsync(string workflowInstanceId, string? activityId = default, WorkflowInput? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified startable workflow.
        /// </summary>
        Task<RunWorkflowResult> ExecuteStartableWorkflowAsync(StartableWorkflow startableWorkflow, WorkflowInput? input = default, CancellationToken cancellationToken = default);        

        /// <summary>
        /// Dispatches the specified startable workflow.
        /// </summary>
        Task<CollectedWorkflow> DispatchStartableWorkflowAsync(StartableWorkflow startableWorkflow, WorkflowInput? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects and executes workflows that are ready for execution. This takes into account both resumable (suspended) workflows as well as startable workflows.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> CollectAndExecuteWorkflowsAsync(WorkflowsQuery query, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Collects and dispatches workflows that are ready for execution. This takes into account both resumable (suspended) workflows as well as startable workflows.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> CollectAndDispatchWorkflowsAsync(WorkflowsQuery query, WorkflowInput? input = default, CancellationToken cancellationToken = default);
    }
}