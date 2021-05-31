using Elsa.Models;

namespace Elsa.Activities.Conductor.Models
{
    public class CommandDefinition : Entity
    {
        public string Name { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }
}