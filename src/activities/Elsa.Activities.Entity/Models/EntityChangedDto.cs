
namespace Elsa.Activities.Entity.Models
{
    public class EntityChangedDto
    {
        public string? EntityName { get; set; }
        public EntityChangedAction? Action { get; set; }

        public EntityChangedDto()
        {

        }

        public EntityChangedDto(string? entityName, EntityChangedAction? action)
        {
            EntityName = entityName;
            Action = action;
        }       
    }
}
