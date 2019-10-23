using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowDefinitionVersionDocument : YesSqlDocument
    {
        public string WorkflowDefinitionVersionId { get; set; }
        public string WorkflowDefinitionId { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; }
        public IList<ConnectionDefinition> Connections { get; set; }
        public Variables Variables { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
    }
}