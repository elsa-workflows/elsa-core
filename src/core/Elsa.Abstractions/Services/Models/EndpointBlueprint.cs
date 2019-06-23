namespace Elsa.Services.Models
{
    public abstract class EndpointBlueprint
    {
        protected EndpointBlueprint(string activityId)
        {
            ActivityId = activityId;
        }
        
        public string ActivityId { get; }
    }
}