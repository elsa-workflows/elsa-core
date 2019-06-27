namespace Elsa.Models
{
    public class SourceEndpointDefinition : EndpointDefinition
    {
        public SourceEndpointDefinition(string activityId, string outcome) : base(activityId)
        {
            Outcome = outcome;
        }
        
        public string Outcome { get; }
    }
}