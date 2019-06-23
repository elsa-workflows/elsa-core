namespace Elsa.Services.Models
{
    public class SourceEndpointBlueprint : EndpointBlueprint
    {
        public SourceEndpointBlueprint(string activityId, string outcome) : base(activityId)
        {
            Outcome = outcome;
        }
        
        public string Outcome { get; }
    }
}