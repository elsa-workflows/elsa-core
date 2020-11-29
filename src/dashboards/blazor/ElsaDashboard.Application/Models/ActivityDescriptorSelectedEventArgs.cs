using System;
using Elsa.Client.Models;

namespace ElsaDashboard.Application.Models
{
    public class ActivityDescriptorSelectedEventArgs : EventArgs
    {
        public ActivityInfo ActivityInfo { get; }

        public ActivityDescriptorSelectedEventArgs(ActivityInfo activityInfo)
        {
            ActivityInfo = activityInfo;
        }
    }
}