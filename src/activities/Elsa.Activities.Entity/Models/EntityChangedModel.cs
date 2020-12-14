
namespace Elsa.Activities.Entity.Models
{
    public class EntityChangedModel
    {
        public EntityChangedModel()
        {
        }

        public EntityChangedModel(string? entityName, EntityChangedAction? action)
        {
            EntityName = entityName;
            Action = action;
        }
        
        public string? EntityName { get; set; }
        public EntityChangedAction? Action { get; set; }
    }
}
