using Elsa.Models;
using System.Collections.Generic;

namespace Elsa.Persistence.Core.Entities
{
    public class WorkflowDefinitionVersionEntity
    {
        public int Id { get; set; }
        public string VersionId { get; set; }= default!;
        public string DefinitionId { get; set; }= default!;
        public int Version { get; set; }
        public string Name { get; set; }= default!;
        public string Description { get; set; }= default!;
        public Variables Variables { get; set; }= default!;
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public ICollection<ActivityDefinitionEntity> Activities { get; set; }= default!;
        public ICollection<ConnectionDefinitionEntity> Connections { get; set; }= default!;
    }
}