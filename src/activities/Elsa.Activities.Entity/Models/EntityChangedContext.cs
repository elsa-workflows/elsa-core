namespace Elsa.Activities.Entity.Models
{
    /// <summary>
    /// Contains information about the entity event and the affected entity.
    /// </summary>
    public class EntityChangedContext
    {
        public EntityChangedContext(string entityId, string entityName, EntityChangedAction action)
        {
            EntityId = entityId;
            EntityName = entityName;
            Action = action;
        }

        public string EntityName { get; }
        public string EntityId { get; }
        public EntityChangedAction Action { get; }
    }
}