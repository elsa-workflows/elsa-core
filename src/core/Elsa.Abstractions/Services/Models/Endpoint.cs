namespace Elsa.Services.Models
{
    public abstract class Endpoint
    {
        protected Endpoint(IActivityBlueprint activity)
        {
            Activity = activity;
        }
        
        public IActivityBlueprint Activity { get; set; }
    }
}