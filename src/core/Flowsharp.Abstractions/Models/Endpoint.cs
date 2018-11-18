namespace Flowsharp.Models
{
    public abstract class Endpoint
    {
        protected Endpoint()
        {
        }

        protected Endpoint(Flowsharp.IActivity activity)
        {
            Activity = activity;
        }
        
        public Flowsharp.IActivity Activity { get; set; }
    }
}