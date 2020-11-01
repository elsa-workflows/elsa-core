using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public interface IWorkflowBlueprint
    {
        public string? Name { get; }
        public string Id { get; }
        public int Version { get; set; }
        public bool IsSingleton { get; }
        public bool IsEnabled { get; }
        public string? Description { get; }
        public bool IsPublished { get; }
        public bool IsLatest { get; }
        public Variables Variables { get; }
        
        /// <summary>
        /// An optional context type around which this workflow revolves. For example, a document, a leave request or a job application.
        /// </summary>
        public WorkflowContextOptions? ContextOptions { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; }
        public bool DeleteCompletedInstances { get; }
        public ICollection<IActivityBlueprint> Activities { get; }
        public ICollection<IConnection> Connections { get; }
        IActivityPropertyProviders ActivityPropertyProviders { get; }
    }
}