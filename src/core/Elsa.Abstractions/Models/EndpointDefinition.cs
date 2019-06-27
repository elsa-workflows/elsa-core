namespace Elsa.Models
{
    public abstract class EndpointDefinition
    {
        protected EndpointDefinition(string activityId)
        {
            ActivityId = activityId;
        }
        
        public string ActivityId { get; }
    }
}