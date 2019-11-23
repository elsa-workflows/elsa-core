namespace Elsa.Server.GraphQL.Models
{
    public class ActivityDefinitionModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public string State { get; set; }
    }
}