using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Server.GraphQL.Models
{
    public class WorkflowDefinitionVersionModel
    {
        public string Id { get; set; }
        public string DefinitionId { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<ActivityDefinitionModel> Activities { get; set; }
        public ICollection<ConnectionDefinition> Connections { get; set; }
        public Variables Variables { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
    }
}