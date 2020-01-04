using Elsa.Services.Models;

namespace Elsa.Activities.Containers
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