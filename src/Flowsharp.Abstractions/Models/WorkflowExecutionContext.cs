using System;
using System.Collections.Generic;
using System.Linq;
using Flowsharp.Descriptors;

namespace Flowsharp.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(IDictionary<string, ActivityDescriptor> activityDescriptorDictionary, WorkflowType workflowType, WorkflowStatus status)
        {
            WorkflowType = workflowType;
            Activities = workflowType.Activities.ToDictionary(x => x.Id, x => new ActivityExecutionContext(x, activityDescriptorDictionary[x.Name]));
            BlockingActivities = new List<ActivityExecutionContext>();
            Status = status;
            IsFirstPass = true;
            
            scheduledActivities = new Stack<ActivityExecutionContext>();
        }
        
        private readonly Stack<ActivityExecutionContext> scheduledActivities;

        public WorkflowType WorkflowType { get; }
        public IDictionary<string, ActivityExecutionContext> Activities { get; }
        public ICollection<ActivityExecutionContext> BlockingActivities { get; }
        public WorkflowStatus Status { get; set; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public ActivityExecutionContext CurrentExecutingActivity { get; private set; }

        public void PushScheduledActivity(ActivityExecutionContext activityExecutionContext)
        {
            scheduledActivities.Push(activityExecutionContext);
        }
        
        public ActivityExecutionContext PopScheduledActivity()
        {
            return CurrentExecutingActivity = scheduledActivities.Pop();
        }

        public void Fault(Exception exception, ActivityExecutionContext activity)
        {
            throw new NotImplementedException();
        }
    }
}
