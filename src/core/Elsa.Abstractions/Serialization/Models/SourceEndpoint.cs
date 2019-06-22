namespace Elsa.Serialization.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(string activityId, string outcome = EndpointNames.Done) : base(activityId)
        {
            Outcome = outcome;
        }

        public string Outcome { get; set; }
    }
}