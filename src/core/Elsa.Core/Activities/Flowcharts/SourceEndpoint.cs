using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Flowcharts
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(IActivity activity, string outcome) : base(activity)
        {
            Outcome = outcome;
        }

        public string Outcome { get; set; }
    }
}