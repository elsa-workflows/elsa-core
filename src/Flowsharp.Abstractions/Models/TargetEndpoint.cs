using Flowsharp.Activities;

namespace Flowsharp.Models
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