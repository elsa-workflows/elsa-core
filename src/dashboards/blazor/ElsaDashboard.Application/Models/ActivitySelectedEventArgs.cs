namespace ElsaDashboard.Application.Models
{
    public class ActivitySelectedEventArgs : ActivityEventArgs
    {
        public ActivitySelectedEventArgs(ActivityModel activityModel) : base(activityModel)
        {
        }
    }
}