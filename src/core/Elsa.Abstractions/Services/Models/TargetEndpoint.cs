namespace Elsa.Services.Models
{
    public class TargetEndpoint : Endpoint
    {
        public TargetEndpoint()
        {
        }

        public TargetEndpoint(IActivity activity) : base(activity)
        {
        }

        public Serialization.Models.TargetEndpoint ToInstance()
        {
            return new Serialization.Models.TargetEndpoint
            {
                ActivityId = Activity.Id
            };
        }
    }
}