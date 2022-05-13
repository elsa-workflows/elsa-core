using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowPublisher
    {
        WorkflowDefinition New();
        Task<WorkflowDefinition?> PublishAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition?> RetractAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> RetractAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition?> GetDraftAsync(string workflowDefinitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Delete the specified workflow version or versions.
        /// </summary>
        Task DeleteAsync(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Delete a specific workflow version.
        /// </summary>
        Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    }

    public static class WorkflowPublisherExtensions
    {
        public static Task<WorkflowDefinition?> GetDraftAsync(this IWorkflowPublisher workflowPublisher, string workflowDefinitionId, CancellationToken cancellationToken = default) =>
            workflowPublisher.GetDraftAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken);
    }
}