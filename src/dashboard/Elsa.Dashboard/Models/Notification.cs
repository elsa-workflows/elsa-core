namespace Elsa.Dashboard.Models
{
    public class Notification
    {
        public Notification()
        {
        }
        
        public Notification(string message, NotificationType type = NotificationType.Information)
        {
            Message = message;
            Type = type;
        }
        
        public string Message { get; set; }
        public NotificationType Type { get; set; }
    }
}