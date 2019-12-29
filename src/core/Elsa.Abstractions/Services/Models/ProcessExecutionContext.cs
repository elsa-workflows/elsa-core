using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;
using NodaTime;

namespace Elsa.Services.Models
{
    public class ProcessExecutionContext
    {
        private readonly IClock clock;

        public ProcessExecutionContext(ProcessInstance processInstance, IExpressionEvaluator expressionEvaluator, IClock clock, IServiceProvider serviceProvider)
        {
            this.clock = clock;
            ProcessInstance = processInstance;
            ServiceProvider = serviceProvider;
            IsFirstPass = true;
            ExpressionEvaluator = expressionEvaluator;
        }

        public ProcessInstance ProcessInstance { get; }
        public IServiceProvider ServiceProvider { get; }
        public bool HasScheduledActivities => ProcessInstance.ScheduledActivities.Any();
        public bool IsFirstPass { get; set; }
        public ProcessExecutionScope CurrentScope => ProcessInstance.Scopes.Peek();
        public ScheduledActivity? ScheduledActivity { get; private set; }

        public void ScheduleActivities(IEnumerable<IActivity> activities, Variable? input = default)
        {
            foreach (var activity in activities) 
                ScheduleActivity(activity, input);
        }

        public void BeginScope() => ProcessInstance.Scopes.Push(new ProcessExecutionScope());
        public void EndScope() => ProcessInstance.Scopes.Pop();

        public void ScheduleActivity(IActivity activity, Variable? input = default)
        {
            ProcessInstance.ScheduledActivities.Push(new ScheduledActivity(activity, input));
        }

        public ScheduledActivity PopScheduledActivity() => ScheduledActivity = ProcessInstance.ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => ProcessInstance.ScheduledActivities.Peek();
        public IExpressionEvaluator ExpressionEvaluator { get; }

        public bool AddBlockingActivity(IActivity activity) => ProcessInstance.BlockingActivities.Add(activity);

        public void SetVariable(string name, object value)
        {
            // Get the first scope (starting from the oldest one) containing the variable (existing variable). Otherwise use the current scope (new variable declaration)
            var scope = ProcessInstance.Scopes.Reverse().FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            scope.SetVariable(name, value);
        }

        public T GetVariable<T>(string name) => (T)GetVariable(name);

        public object GetVariable(string name)
        {
            // Get the first scope (starting from the newest one) containing the variable.
            var scope = ProcessInstance.Scopes.FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            return scope.GetVariable(name);
        }

        public Variables GetVariables() => ProcessInstance.Scopes
            .Reverse()
            .Select(x => x.Variables)
            .Aggregate(Variables.Empty, (x, y) => new Variables(x.Union(y)));

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            return ExpressionEvaluator.EvaluateAsync(expression, activityExecutionContext, cancellationToken);
        }

        public Task<object> EvaluateAsync(IWorkflowExpression expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) =>
            ExpressionEvaluator.EvaluateAsync(expression, activityExecutionContext, cancellationToken);

        public void Run()
        {
            ProcessInstance.StartedAt = clock.GetCurrentInstant();
            ProcessInstance.Status = ProcessStatus.Running;
        }

        public void Fault(IActivity activity, Exception exception) => Fault(activity, exception.Message);

        public void Fault(IActivity activity, string errorMessage)
        {
            ProcessInstance.FaultedAt = clock.GetCurrentInstant();
            ProcessInstance.Fault = new ProcessFault(activity, errorMessage);
            ProcessInstance.Status = ProcessStatus.Faulted;
        }

        public void Suspend()
        {
            ProcessInstance.Status = ProcessStatus.Suspended;
        }

        public void Complete()
        {
            ProcessInstance.CompletedAt = clock.GetCurrentInstant();
            ProcessInstance.Status = ProcessStatus.Completed;
            ProcessInstance.BlockingActivities.Clear();
        }

        public void Cancel()
        {
            ProcessInstance.CancelledAt = clock.GetCurrentInstant();
            ProcessInstance.Status = ProcessStatus.Cancelled;
        }
    }
}