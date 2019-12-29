using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Models
{
    public class TargetEndpoint : Endpoint
    {
        public TargetEndpoint()
        {
        }

        public TargetEndpoint(IActivity activity) : base(activity)
        {
        }
    }
}