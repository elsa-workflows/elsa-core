using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowExecutionContext
    {
        private readonly Stack<IActivity> scheduledActivities;

        public WorkflowExecutionContext(Workflow workflow)
        {
            Workflow = workflow;
            IsFirstPass = true;
            scheduledActivities = new Stack<IActivity>();
        }

        public Workflow Workflow { get; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public IActivity CurrentActivity { get; private set; }

        public WorkflowExecutionScope CurrentScope
        {
            get => Workflow.CurrentScope;
            set => Workflow.CurrentScope = value;
        }
        
        public ActivityExecutionContext CreateActivityExecutionContext(IActivity activity) => 
            new ActivityExecutionContext(activity);

        public void BeginScope()
        {
            Workflow.Scopes.Push(CurrentScope = new WorkflowExecutionScope());
        }

        public void EndScope()
        {
            Workflow.Scopes.Pop();
            CurrentScope = Workflow.Scopes.Peek();
        }

        public void ScheduleActivity(IActivity activity)
        {
            scheduledActivities.Push(activity);
        }

        public IActivity PopScheduledActivity()
        {
            CurrentActivity = scheduledActivities.Pop();
            return CurrentActivity;
        }

        public void SetLastResult(object value)
        {
            CurrentScope.LastResult = value;
        }

        public void Fault(Exception exception, IActivity activity, Instant instant) => Fault(exception.Message, activity, instant);
        
        public void Fault(string errorMessage, IActivity activity, Instant instant)
        {
            Workflow.FinishedAt = instant;
            Workflow.Fault = new WorkflowFault
            {
                Message = errorMessage,
                FaultedActivity = activity
            };
        }

        public void Halt(Instant instant)
        {
            var activity = CurrentActivity;
            if (!Workflow.BlockingActivities.Contains(activity))
            {
                Workflow.BlockingActivities.Add(activity);
            }

            Workflow.HaltedAt = instant;
            Workflow.Status = WorkflowStatus.Halted;
        }

        public void Finish(Instant instant)
        {
            Workflow.FinishedAt = instant;
            Workflow.Status = WorkflowStatus.Finished;
        }

        public void ScheduleNextActivities(WorkflowExecutionContext workflowContext, SourceEndpoint endpoint)
        {
            var completedActivity = workflowContext.CurrentActivity;
            var connections = Workflow.Connections.Where(x => x.Source.Activity.Id == completedActivity.Id && x.Source.Name == endpoint.Name);

            foreach (var connection in connections)
            {
                workflowContext.ScheduleActivity(connection.Target.Activity);
            }
        }
    }
}