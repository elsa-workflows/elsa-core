using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowExecutionContext
    {
        private readonly IClock clock;
        private readonly Stack<IActivity> scheduledActivities;
        private readonly Stack<IActivity> scheduledHaltingActivities;

        public WorkflowExecutionContext(Workflow workflow, IClock clock, IServiceProvider serviceProvider)
        {
            this.clock = clock;
            Workflow = workflow;
            ServiceProvider = serviceProvider;
            IsFirstPass = true;
            scheduledActivities = new Stack<IActivity>();
            scheduledHaltingActivities = new Stack<IActivity>();
        }

        public Workflow Workflow { get; }
        public IServiceProvider ServiceProvider { get; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool HasScheduledHaltingActivities => scheduledHaltingActivities.Any();
        public bool IsFirstPass { get; set; }
        public IActivity CurrentActivity { get; private set; }
        public LogEntry CurrentLogEntry => Workflow.ExecutionLog.LastOrDefault();

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

        public void ScheduleHaltingActivity(IActivity activity)
        {
            scheduledHaltingActivities.Push(activity);
        }

        public IActivity PopScheduledHaltingActivity()
        {
            return scheduledHaltingActivities.Pop();
        }

        public void SetLastResult(object value)
        {
            CurrentScope.LastResult = value;
        }

        public void Fault(Exception exception, IActivity activity) => Fault(exception.Message, activity);

        public void Fault(string errorMessage, IActivity activity)
        {
            Workflow.FinishedAt = clock.GetCurrentInstant();
            Workflow.Fault = new WorkflowFault
            {
                Message = errorMessage,
                FaultedActivity = activity
            };
        }

        public void Halt(IActivity activity = null)
        {
            if (activity != null)
                if (!Workflow.BlockingActivities.Contains(activity))
                    Workflow.BlockingActivities.Add(activity);

            Workflow.HaltedAt = clock.GetCurrentInstant();
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
            var connections = Workflow.Connections.Where(x => x.Source.Activity.Id == completedActivity.Id && x.Source.Outcome == endpoint.Outcome);

            foreach (var connection in connections)
            {
                workflowContext.ScheduleActivity(connection.Target.Activity);
            }
        }
    }
}