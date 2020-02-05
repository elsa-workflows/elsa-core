using Elsa.Models;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionInput
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int? Left { get; set; }
        public int? Top { get; set; }
        public Variables? State { get; set; }
    }
}