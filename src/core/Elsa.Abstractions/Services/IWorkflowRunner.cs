using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Triggers;

namespace Elsa.Services
{
    public interface IWorkflowRunner
    {
        Task StartWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);

        Task StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
        
        Task ResumeWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default, 
            string? correlationId = default, 
            string? contextId = default,
            CancellationToken cancellationToken = default);

        Task ResumeWorkflowsAsync(
            IEnumerable<BookmarkFinderResult> results,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow;

        ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default) where T : IWorkflow;

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);
    }
}