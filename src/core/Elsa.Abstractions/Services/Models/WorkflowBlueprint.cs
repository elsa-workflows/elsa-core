using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class WorkflowBlueprint : CompositeActivityBlueprint, IWorkflowBlueprint
    {
        public WorkflowBlueprint()
        {
            ActivityPropertyProviders = new ActivityPropertyProviders();
            Variables = new Variables();
        }
    
        public WorkflowBlueprint(
            string id,
            int version,
            bool isSingleton,
            bool isEnabled,
            string? name,
            string? description,
            bool isLatest,
            bool isPublished,
            Variables? variables,
            WorkflowContextOptions? contextOptions,
            WorkflowPersistenceBehavior persistenceBehavior,
            bool deleteCompletedInstances,
            IEnumerable<IActivityBlueprint> activities,
            IEnumerable<IConnection> connections,
            IActivityPropertyProviders activityPropertyValueProviders) : base(id, name, id, true, null!)
        {
            Id = id;
            Version = version;
            IsSingleton = isSingleton;
            IsEnabled = isEnabled;
            IsLatest = isLatest;
            IsPublished = isPublished;
            ContextOptions = contextOptions;
            Variables = variables ?? new Variables();
            Name = name;
            Description = description;
            PersistenceBehavior = persistenceBehavior;
            DeleteCompletedInstances = deleteCompletedInstances;
            Activities = activities.ToList();
            Connections = connections.ToList();
            ActivityPropertyProviders = activityPropertyValueProviders;
        }
        
        public int Version { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsEnabled { get; set; }
        public string? Description { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public Variables Variables { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
    }
}