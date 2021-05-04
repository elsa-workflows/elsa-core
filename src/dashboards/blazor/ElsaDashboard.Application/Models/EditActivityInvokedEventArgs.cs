namespace ElsaDashboard.Application.Models
{
    public class EditActivityInvokedEventArgs : ActivityEventArgs
    {
        public EditActivityInvokedEventArgs(ActivityModel activityModel) : base(activityModel)
        {
        }
    }
}