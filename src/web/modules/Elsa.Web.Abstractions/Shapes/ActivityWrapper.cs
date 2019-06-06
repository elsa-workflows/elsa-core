using System.Collections.Generic;
using Elsa.Models;
using Elsa.Web.Extensions;
using Elsa.Web.Models;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Shapes
{
    public class ActivityWrapper : ActivityShape
    {
        public ActivityWrapper(
            IActivity activity, 
            ActivityDescriptor activityDescriptor, 
            ActivityDesignerDescriptor activityDesignerDescriptor,
            IShape content) : base(activity, activityDescriptor, activityDesignerDescriptor, "Activity_Wrapper")
        {
            Content = content;
            ActivityDesignerMetadata = activity.GetDesignerMetadata();
            LogEntries = new List<LogEntry>();
            BlockingActivities = new List<IActivity>();
        }

        public ActivityDesignerMetadata ActivityDesignerMetadata { get; }
        public ICollection<LogEntry> LogEntries { get; set; }
        public IShape Content { get; }
        public ICollection<IActivity> BlockingActivities { get; set; }
        public bool HasExecuted { get; set; }
        public bool HasFaulted { get; set; }
        public bool IsBlocking { get; set; }
        public bool WorkflowIsDefinition { get; set; }
    }
}