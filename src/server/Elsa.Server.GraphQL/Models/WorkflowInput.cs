using System.Collections.Generic;
using Elsa.Models;
using Elsa.Server.GraphQL.Types;

namespace Elsa.Server.GraphQL.Models
{
    public class WorkflowInput
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsSingleton { get; set; }
        public bool? IsDisabled { get; set; }
        public bool? DeleteCompletedInstances { get; set; }
        public Variables? Variables { get; set; }
        public WorkflowPersistenceBehavior? PersistenceBehavior { get; set; }
        public ICollection<ActivityDefinitionInput>? Activities { get; set; }
        public ICollection<ConnectionDefinition>? Connections { get; set; }
    }
}