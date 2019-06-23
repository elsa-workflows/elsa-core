using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Models;
using NodaTime;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        private readonly IClock clock;
        private readonly Stack<IActivity> scheduledActivities;
        private readonly Stack<IActivity> scheduledHaltingActivities;

        public WorkflowExecutionContext(Workflow workflowInstance, IClock clock, IServiceProvider serviceProvider)
        {
            this.clock = clock;
            Workflow = workflowInstance;
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
        public LogEntry CurrentLogEntry => Workflow.ExecutionLog.LastOrDefault();
        public WorkflowExecutionScope CurrentScope => Workflow.Scopes.Peek();
        public IActivity CurrentActivity { get; private set; }

        public ActivityExecutionContext CreateActivityExecutionContext(IActivity activity) => new ActivityExecutionContext(activity);
        public Task ScheduleActivitiesAsync(params IActivity[] activities) => ScheduleActivitiesAsync((IEnumerable<IActivity>) activities);

        public async Task ScheduleActivitiesAsync(IEnumerable<IActivity> activities)
        {
            foreach (var activity in activities)
            {
                if(await activity.CanExecuteAsync(this))
                    ScheduleActivity(activity);
            }
        }

        public void BeginScope() => Workflow.Scopes.Push(new WorkflowExecutionScope());
        public void EndScope() => Workflow.Scopes.Pop();
        public void ScheduleActivity(IActivity activity) => scheduledActivities.Push(activity);
        public IActivity PopScheduledActivity() => CurrentActivity = scheduledActivities.Pop();
        public void ScheduleHaltingActivity(IActivity activity) => scheduledHaltingActivities.Push(activity);
        public IActivity PopScheduledHaltingActivity() => scheduledHaltingActivities.Pop();
        public void SetVariable(string name, object value) => CurrentScope.SetVariable(name, value);
        public T GetVariable<T>(string name) => CurrentScope.GetVariable<T>(name);
        public void SetLastResult(object value) => CurrentScope.LastResult = value;

        public void Fault(IActivity activity, Exception exception) => Fault(activity, exception.Message);

        public void Fault(IActivity activity, string errorMessage)
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
                Workflow.BlockingActivities.Add(activity);

            Workflow.HaltedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Halted;
        }

        public void Finish(Instant instant)
        {
            Workflow.FinishedAt = instant;
            Workflow.Status = WorkflowStatus.Finished;
        }
    }
}