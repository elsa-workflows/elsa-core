﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;
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
        Task<IEnumerable<CollectedWorkflow>> CollectWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects and creates workflow instances for startable workflows.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<StartableWorkflow>> CollectStartableWorkflowsAsync(CollectWorkflowsContext context, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new workflow instance with execution pending for the specified workflow blueprint using the specified starting activity ID.
        /// </summary>
        Task<StartableWorkflow?> CollectStartableWorkflowAsync(string workflowDefinitionId, string? activityId = default, string? correlationId = default, string? contextId = default, string? tenantId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new workflow instance with execution pending for the specified workflow blueprint using the specified starting activity ID.
        /// </summary>
        Task<StartableWorkflow?> CollectStartableWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string? activityId = default, string? correlationId = default, string? contextId = default, string? tenantId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Collects and executes the specified startable workflow.
        /// </summary>
        Task CollectAndExecuteStartableWorkflowAsync(string workflowDefinitionId, string? activityId = default, string? correlationId = default, string? contextId = default, object? input = default, string? tenantId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Collects and executes the specified startable workflow.
        /// </summary>
        Task<RunWorkflowResult> CollectAndExecuteStartableWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string? activityId = default, string? correlationId = default, string? contextId = default, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a list of pending workflows.
        /// </summary>
        Task ExecutePendingWorkflowsAsync(IEnumerable<CollectedWorkflow> pendingWorkflows, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a pending workflow.
        /// </summary>
        Task ExecutePendingWorkflowAsync(CollectedWorkflow collectedWorkflow, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a pending workflow.
        /// </summary>
        Task<RunWorkflowResult> ExecutePendingWorkflowAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a list of pending workflows for execution.
        /// </summary>
        Task DispatchPendingWorkflowsAsync(IEnumerable<CollectedWorkflow> pendingWorkflows, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a pending workflow for execution.
        /// </summary>
        Task DispatchPendingWorkflowAsync(CollectedWorkflow collectedWorkflow, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Dispatches a pending workflow for execution.
        /// </summary>
        Task DispatchPendingWorkflowAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified startable workflow.
        /// </summary>
        Task<RunWorkflowResult> ExecuteStartableWorkflowAsync(StartableWorkflow startableWorkflow, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Dispatches the specified startable workflow.
        /// </summary>
        Task<CollectedWorkflow> DispatchStartableWorkflowAsync(StartableWorkflow startableWorkflow, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects and executes workflows that are ready for execution. This takes into account both resumable (suspended) workflows as well as startable workflows.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> CollectAndExecuteWorkflowsAsync(CollectWorkflowsContext context, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Collects and dispatches workflows that are ready for execution. This takes into account both resumable (suspended) workflows as well as startable workflows.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> CollectAndDispatchWorkflowsAsync(CollectWorkflowsContext context, object? input = default, CancellationToken cancellationToken = default);
    }

    public record CollectWorkflowsContext(string ActivityType, IBookmark? Bookmark, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default);

    public record CollectStartableWorkflowsContext(string WorkflowDefinitionId, string? ActivityId = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default);
}