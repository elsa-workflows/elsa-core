namespace Elsa.Services.Models
{
    public class TargetEndpoint : Endpoint, ITargetEndpoint
    {
        public TargetEndpoint(IActivity activity) : base(activity)
        {
        }
    }
}