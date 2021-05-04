using System;

namespace ElsaDashboard.Application.Models
{
    public abstract class ActivityEventArgs : EventArgs
    {
        public ActivityModel ActivityModel { get; }
        protected ActivityEventArgs(ActivityModel activityModel) => ActivityModel = activityModel;
    }
}