using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Activities.Conductor.Models
{
    public class EventDefinition : Entity
    {
        public string Name { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public ICollection<string>? Outcomes { get; set; }
    }
}