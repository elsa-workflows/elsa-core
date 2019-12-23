using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Scripting;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        private readonly IClock clock;

        public WorkflowExecutionContext(Workflow workflow, IWorkflowExpressionEvaluator workflowExpressionEvaluator, IClock clock, IServiceProvider serviceProvider)
        {
            this.clock = clock;
            Workflow = workflow;
            ServiceProvider = serviceProvider;
            IsFirstPass = true;
            WorkflowExpressionEvaluator = workflowExpressionEvaluator;
        }

        public Workflow Workflow { get; }
        public IServiceProvider ServiceProvider { get; }
        public bool HasScheduledActivities => Workflow.ScheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public WorkflowExecutionScope CurrentScope => Workflow.Scopes.Peek();
        public IActivity CurrentActivity { get; private set; }
        public void ScheduleActivities(params IActivity[] activities) => ScheduleActivities((IEnumerable<IActivity>)activities);

        public void ScheduleActivities(IEnumerable<IActivity> activities)
        {
            foreach (var activity in activities)
            {
                ScheduleActivity(activity);
            }
        }

        public void BeginScope() => Workflow.Scopes.Push(new WorkflowExecutionScope());
        public void EndScope() => Workflow.Scopes.Pop();

        public void ScheduleActivity(IActivity activity)
        {
            Workflow.ScheduledActivities.Push(activity);
        }
        
        public IActivity PopScheduledActivity() => CurrentActivity = Workflow.ScheduledActivities.Pop();
        public IActivity PeekScheduledActivity() => Workflow.ScheduledActivities.Peek();
        public IWorkflowExpressionEvaluator WorkflowExpressionEvaluator { get; }

        public bool AddBlockingActivity(IActivity activity) => Workflow.BlockingActivities.Add(activity);

        public void SetVariable(string name, object value)
        {
            // Get the first scope (starting from the oldest one) containing the variable (existing variable). Otherwise use the current scope (new variable declaration)
            var scope = Workflow.Scopes.Reverse().FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            scope.SetVariable(name, value);
        }

        public T GetVariable<T>(string name) => (T) GetVariable(name);

        public object GetVariable(string name)
        {
            // Get the first scope (starting from the newest one) containing the variable.
            var scope = Workflow.Scopes.FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            return scope.GetVariable(name);
        }
        
        public Variables GetVariables() => Workflow.Scopes
            .Reverse()
            .Select(x => x.Variables)
            .Aggregate(Variables.Empty, (x, y) => new Variables(x.Union(y)));

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, CancellationToken cancellationToken)
        {
            return WorkflowExpressionEvaluator.EvaluateAsync(expression, this, cancellationToken);
        }

        public Task<object> EvaluateAsync(IWorkflowExpression expression, Type type, CancellationToken cancellationToken) =>
            WorkflowExpressionEvaluator.EvaluateAsync(expression, this, cancellationToken);

        public void Run()
        {
            Workflow.StartedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Running;
        }

        public void Fault(IActivity activity, Exception exception) => Fault(activity, exception.Message);

        public void Fault(IActivity activity, string errorMessage)
        {
            Workflow.FaultedAt = clock.GetCurrentInstant();
            Workflow.Fault = new WorkflowFault
            {
                Message = errorMessage,
                FaultedActivity = activity
            };
            Workflow.Status = WorkflowStatus.Faulted;
        }

        public void Suspend()
        {
            Workflow.Status = WorkflowStatus.Suspended;
        }

        public void Complete()
        {
            Workflow.CompletedAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Completed;
            Workflow.BlockingActivities.Clear();
        }

        public void Cancel()
        {
            Workflow.CancelledAt = clock.GetCurrentInstant();
            Workflow.Status = WorkflowStatus.Cancelled;
        }
    }
}