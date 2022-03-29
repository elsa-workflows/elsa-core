using Elsa.Models;

namespace Elsa.Secrets.Models
{
    public class Secret : Entity
    {
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string PropertiesJson { get; set; }
    }
}
