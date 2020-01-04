using Elsa.Services.Models;

namespace Elsa.Activities.Containers
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

        public string? Outcome { get; set; }
    }
}