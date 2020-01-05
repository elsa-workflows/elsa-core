using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Flowcharts
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