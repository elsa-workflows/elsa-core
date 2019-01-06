namespace Elsa.Models
{
    public class ActivityFault
    {
        public ActivityFault(string message)
        {
            Message = message;
        }
        
        public string Message { get; set; }
    }
}