namespace Elsa.Services.Models
{
    public class TargetEndpoint : Endpoint, ITargetEndpoint
    {
        public TargetEndpoint(IActivityBlueprint activity) : base(activity)
        {
        }
    }
}