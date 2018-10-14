using Flowsharp.Activities;

namespace Flowsharp.Models
{
    public abstract class Endpoint
    {
        protected Endpoint()
        {
        }

        protected Endpoint(IActivity activity)
        {
            Activity = activity;
        }
        
        public IActivity Activity { get; set; }
    }
}