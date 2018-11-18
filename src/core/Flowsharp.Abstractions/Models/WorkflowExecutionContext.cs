using System;
using System.Collections.Generic;
using System.Linq;

namespace Flowsharp.Models
{
    public class WorkflowExecutionContext
    {
        private readonly IDictionary<string, IActivity> activityDescriptors;
        private readonly Stack<Flowsharp.IActivity> scheduledActivities;

        public WorkflowExecutionContext(Workflow workflow, IDictionary<string, IActivity> activityDescriptors)
        {
            this.activityDescriptors = activityDescriptors;
            Workflow = workflow;
            IsFirstPass = true;
            scheduledActivities = new Stack<Flowsharp.IActivity>();
        }

        public Workflow Workflow { get; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public Flowsharp.IActivity CurrentActivity { get; private set; }

        public WorkflowExecutionScope CurrentScope
        {
            get => Workflow.CurrentScope;
            set => Workflow.CurrentScope = value;
        }
        
        public ActivityExecutionContext CreateActivityExecutionContext(Flowsharp.IActivity activity) => 
            new ActivityExecutionContext(activity, activityDescriptors[activity.Name]);

        public void BeginScope()
        {
            Workflow.Scopes.Push(CurrentScope = new WorkflowExecutionScope());
        }

        public void EndScope()
        {
            Workflow.Scopes.Pop();
            CurrentScope = Workflow.Scopes.Peek();
        }

        public void ScheduleActivity(Flowsharp.IActivity activity)
        {
            scheduledActivities.Push(activity);
        }

        public Flowsharp.IActivity PopScheduledActivity()
        {
            CurrentActivity = scheduledActivities.Pop();
            return CurrentActivity;
        }

        public void SetLastResult(object value)
        {
            CurrentScope.LastResult = value;
        }

        public void Fault(Exception exception, Flowsharp.IActivity activity) => Fault(exception.Message, activity);
        
        public void Fault(string errorMessage, Flowsharp.IActivity activity)
        {
            Workflow.Fault = new WorkflowFault
            {
                Message = errorMessage,
                FaultedActivity = activity
            };
        }

        public void Halt()
        {
            var activity = CurrentActivity;
            if (!Workflow.BlockingActivities.Contains(activity))
            {
                Workflow.BlockingActivities.Add(activity);
            }

            Workflow.Status = WorkflowStatus.Halted;
        }

        public void Finish()
        {
            Workflow.Status = WorkflowStatus.Finished;
        }

        public void ScheduleNextActivities(WorkflowExecutionContext workflowContext, SourceEndpoint endpoint)
        {
            var completedActivity = workflowContext.CurrentActivity;
            var connections = Workflow.Connections.Where(x => x.Source.Activity == completedActivity && x.Source.Name == endpoint.Name);

            foreach (var connection in connections)
            {
                workflowContext.ScheduleActivity(connection.Target.Activity);
            }
        }
    }
}