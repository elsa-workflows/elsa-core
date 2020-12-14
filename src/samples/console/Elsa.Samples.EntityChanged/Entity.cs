using Elsa.Activities.Entity.Attributes;

namespace Elsa.Samples.EntityChanged
{
    [EntityName("Test-Entity")]
    public class Entity
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}
