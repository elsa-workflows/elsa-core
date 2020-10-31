using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class WorkflowBlueprint : IWorkflowBlueprint
    {
        public WorkflowBlueprint()
        {
            Activities = new List<IActivityBlueprint>();
            Connections = new List<IConnection>();
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
            Type? contextType,
            WorkflowPersistenceBehavior persistenceBehavior,
            bool deleteCompletedInstances,
            IEnumerable<IActivityBlueprint> activities,
            IEnumerable<IConnection> connections,
            IActivityPropertyProviders activityPropertyValueProviders)
        {
            Id = id;
            Version = version;
            IsSingleton = isSingleton;
            IsEnabled = isEnabled;
            IsLatest = isLatest;
            IsPublished = isPublished;
            ContextType = contextType;
            Variables = variables ?? new Variables();
            Name = name;
            Description = description;
            PersistenceBehavior = persistenceBehavior;
            DeleteCompletedInstances = deleteCompletedInstances;
            Activities = activities.ToList();
            Connections = connections.ToList();
            ActivityPropertyProviders = activityPropertyValueProviders;
        }
    
        public string Id { get; set; } = default!;
        public int Version { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsEnabled { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public Variables Variables { get; set; }
        public Type? ContextType { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<IActivityBlueprint> Activities { get; set; }
    
        public ICollection<IConnection> Connections { get; set; }
    
        public IActivityPropertyProviders ActivityPropertyProviders { get; set; }
    }
}