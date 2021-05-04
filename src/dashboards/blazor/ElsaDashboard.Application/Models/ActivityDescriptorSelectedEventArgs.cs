using System;
using Elsa.Client.Models;

namespace ElsaDashboard.Application.Models
{
    public class ActivityDescriptorSelectedEventArgs : EventArgs
    {
        public ActivityDescriptor ActivityDescriptor { get; }

        public ActivityDescriptorSelectedEventArgs(ActivityDescriptor activityDescriptor)
        {
            ActivityDescriptor = activityDescriptor;
        }
    }
}