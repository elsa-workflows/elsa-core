namespace Elsa.Services.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(IActivity activity, string outcome = OutcomeNames.Done) : base(activity)
        {
            Outcome = outcome;
        }

        public string Outcome { get; set; }

        public Serialization.Models.SourceEndpoint ToInstance()
        {
            return new Serialization.Models.SourceEndpoint
            {
                ActivityId = Activity.Id,
                Outcome = Outcome
            };
        }
    }
}