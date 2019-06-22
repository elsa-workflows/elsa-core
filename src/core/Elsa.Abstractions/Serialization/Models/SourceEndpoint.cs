namespace Elsa.Serialization.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(string activityId, string outcome) : base(activityId)
        {
            Outcome = outcome;
        }
        
        public string Outcome { get; set; }
    }
}