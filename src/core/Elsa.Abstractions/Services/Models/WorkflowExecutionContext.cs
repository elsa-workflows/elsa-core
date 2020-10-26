using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;
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
            ExpressionEvaluator = serviceProvider.GetRequiredService<IWorkflowExpressionEvaluator>();
        }

        public Workflow Workflow { get; }
        public IServiceProvider ServiceProvider { get; }
        public bool HasScheduledActivities => scheduledActivities.Any();
        public bool HasScheduledHaltingActivities => scheduledHaltingActivities.Any();
        public IEnumerable<IActivity> ScheduledActivities => scheduledActivities;
        public bool IsFirstPass { get; set; }
        public LogEntry CurrentLogEntry => Workflow.ExecutionLog.LastOrDefault();
        public WorkflowExecutionScope CurrentScope => Workflow.Scope;
        public Variables TransientState { get; } = new Variables();
        public IActivity CurrentActivity { get; private set; }
        public void ScheduleActivities(params IActivity[] activities) => ScheduleActivities((IEnumerable<IActivity>)activities);

        public void ScheduleActivities(IEnumerable<IActivity> activities)
        {
            foreach (var activity in activities)
            {
                ScheduleActivity(activity);
            }
        }

        public void ScheduleActivity(IActivity activity)
        {
            scheduledActivities.Push(activity);
        }

        public IActivity PeekScheduledActivity() => scheduledActivities.Peek();
        public IActivity PopScheduledActivity() => CurrentActivity = scheduledActivities.Pop();
        public void ScheduleHaltingActivity(IActivity activity) => scheduledHaltingActivities.Push(activity);
        public IActivity PopScheduledHaltingActivity() => scheduledHaltingActivities.Pop();
        public IWorkflowExpressionEvaluator ExpressionEvaluator { get; }

        public void SetVariable(string name, object value) => CurrentScope.SetVariable(name, value);
        public T GetVariable<T>(string name) => (T) GetVariable(name);
        public object GetVariable(string name) => CurrentScope.GetVariable(name);

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, CancellationToken cancellationToken) =>
            ExpressionEvaluator.EvaluateAsync(expression, this, cancellationToken);

        public void SetLastResult(object value) => SetLastResult(new Variable(value));
        public void SetLastResult(Variable value) => CurrentScope.LastResult = value;

        public void Start()
        {
            Workflow.StartedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Executing;
        }

        public void Fault(IActivity activity, Exception exception) => Fault(activity, exception.Message, exception);

        public void Fault(IActivity activity, string errorMessage, Exception exception = null)
        {
            Workflow.FaultedAt = clock.GetCurrentInstant();
            Workflow.Fault = new WorkflowFault
            {
                Message = errorMessage,
                Exception = exception,
                FaultedActivity = activity
            };
            Workflow.Status = WorkflowStatus.Faulted;
        }

        public void Halt(IActivity activity = null)
        {
            if (activity != null)
                Workflow.BlockingActivities.Add(activity);
        }

        public void Finish()
        {
            Workflow.FinishedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Finished;
            Workflow.BlockingActivities.Clear();
        }

        public void Abort()
        {
            Workflow.AbortedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Aborted;
        }

        public Variables GetVariables() => CurrentScope.Variables;
    }
}