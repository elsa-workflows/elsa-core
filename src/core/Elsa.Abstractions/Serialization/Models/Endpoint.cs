namespace Elsa.Serialization.Models
{
    public abstract class Endpoint
    {
        protected Endpoint()
        {
        }

        protected Endpoint(string activityId)
        {
            ActivityId = activityId;
        }
        
        public string ActivityId { get; set; }
    }
}