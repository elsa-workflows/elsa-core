using Elsa.Models;

namespace Elsa.Services.Models
{
    public interface IWorkflowBlueprint : ICompositeActivityBlueprint
    {
        string VersionId { get; }
        int Version { get; }
        string? TenantId { get; }
        bool IsSingleton { get; }
        bool IsPublished { get; }
        bool IsLatest { get; }
        bool IsDisabled { get; set; }
        string? Tag { get; }
        string? Channel { get; }

        /// <summary>
        /// An initial set of variables available to workflow instances.
        /// </summary>
        Variables Variables { get; }
        
        /// <summary>
        /// An optional context type around which this workflow revolves. For example, a document, a leave request or a job application.
        /// </summary>
        WorkflowContextOptions? ContextOptions { get; set; }
        
        WorkflowPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        
        /// <summary>
        /// A dictionary to store application-specific properties for a given workflow. 
        /// </summary>
        Variables CustomAttributes { get; }
    }
}