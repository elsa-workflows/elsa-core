namespace Elsa.Indexing.Models
{
    public class ActivityDefinitionIndexModel
    {
        public string ActivityId { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }
}
