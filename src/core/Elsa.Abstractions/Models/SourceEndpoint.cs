namespace Elsa.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(IActivity activity, string outcome = EndpointNames.Done) : base(activity)
        {
            Outcome = outcome;
        }

        public string Outcome { get; set; }
    }
}