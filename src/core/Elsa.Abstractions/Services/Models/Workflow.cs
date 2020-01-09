using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class Workflow
    {
        public Workflow()
        {
        }

        public Workflow(
            string definitionId = default,
            int version = 1,
            bool isSingleton = false,
            bool isDisabled = false,
            string? name = default,
            string? description = default,
            bool isLatest = false,
            bool isPublished = false,
            IEnumerable<IActivity>? activities = default,
            IEnumerable<Connection>? connections = default)
        {
            DefinitionId = definitionId;
            Version = version;
            IsSingleton = isSingleton;
            IsDisabled = isDisabled;
            IsLatest = isLatest;
            IsPublished = isPublished;
            Name = name;
            Description = description;
            Activities = activities?.ToList() ?? new List<IActivity>();
            Connections = connections?.ToList() ?? new List<Connection>();
        }

        public string? DefinitionId { get; set; }
        public int Version { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<IActivity> Activities { get; set; }

        public ICollection<Connection> Connections { get; set; }
    }
}