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
            CustomAttributes = new Variables();
        }
    
        public WorkflowBlueprint(
            string id,
            int version,
            string? tenantId,
            bool isSingleton,
            bool isEnabled,
            string? name,
            string? displayName,
            string? description,
            bool isLatest,
            bool isPublished,
            Variables? variables,
            Variables? customAttributes,
            WorkflowContextOptions? contextOptions,
            WorkflowPersistenceBehavior persistenceBehavior,
            bool deleteCompletedInstances,
            IEnumerable<IActivityBlueprint> activities,
            IEnumerable<IConnection> connections,
            IActivityPropertyProviders activityPropertyValueProviders) : base(id, default, name, displayName, description, id, true, false, false)
        {
            Id = id;
            Parent = this;
            Version = version;
            TenantId = tenantId;
            IsSingleton = isSingleton;
            IsEnabled = isEnabled;
            IsLatest = isLatest;
            IsPublished = isPublished;
            ContextOptions = contextOptions;
            Variables = variables ?? new Variables();
            CustomAttributes = customAttributes ?? new Variables();
            Name = name;
            PersistenceBehavior = persistenceBehavior;
            DeleteCompletedInstances = deleteCompletedInstances;
            Activities = activities.ToList();
            Connections = connections.ToList();
            ActivityPropertyProviders = activityPropertyValueProviders;
        }
        
        public int Version { get; set; }
        public string? TenantId { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public Variables Variables { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public Variables CustomAttributes { get; }
    }
}