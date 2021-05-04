namespace ElsaDashboard.Application.Models
{
    public class DeleteActivityInvokedEventArgs : ActivityEventArgs
    {
        public DeleteActivityInvokedEventArgs(ActivityModel activityModel) : base(activityModel)
        {
        }
    }
}