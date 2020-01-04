using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using NodaTime;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(IExpressionEvaluator expressionEvaluator, IServiceProvider serviceProvider, string instanceId)
        {
            ServiceProvider = serviceProvider;
            InstanceId = instanceId;
            ExpressionEvaluator = expressionEvaluator;
            ScheduledActivities = new Stack<ScheduledActivity>();
            BlockingActivities = new HashSet<IActivity>();
            Scopes = new Stack<WorkflowExecutionScope>();
            Status = WorkflowStatus.Running;
            BeginScope();
        }

        public WorkflowStatus Status { get; set; }
        public IServiceProvider ServiceProvider { get; }
        public Stack<ScheduledActivity> ScheduledActivities { get; }
        public HashSet<IActivity> BlockingActivities { get; }
        public Stack<WorkflowExecutionScope> Scopes { get; }
        public bool HasScheduledActivities => ScheduledActivities.Any();
        public WorkflowExecutionScope CurrentScope => Scopes.Peek();
        public ScheduledActivity? ScheduledActivity { get; private set; }

        public void ScheduleActivities(IEnumerable<IActivity> activities, Variable? input = default)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity, input);
        }
        
        public void ScheduleActivities(IEnumerable<ScheduledActivity> activities)
        {
            foreach (var activity in activities)
                ScheduleActivity(activity);
        }

        public void BeginScope() => Scopes.Push(new WorkflowExecutionScope());
        public void EndScope() => Scopes.Pop();

        public void ScheduleActivity(IActivity activity, object? input = default) => ScheduleActivity(new ScheduledActivity(activity, input));
        public void ScheduleActivity(ScheduledActivity activity) => ScheduledActivities.Push(activity);

        public ScheduledActivity PopScheduledActivity() => ScheduledActivity = ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => ScheduledActivities.Peek();
        public IExpressionEvaluator ExpressionEvaluator { get; }
        public string InstanceId { get; set; }
        public string CorrelationId { get; set; }

        public bool AddBlockingActivity(IActivity activity) => BlockingActivities.Add(activity);

        public void SetVariable(string name, object value)
        {
            var scope = Scopes.Reverse().FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            scope.SetVariable(name, value);
        }

        public T GetVariable<T>(string name) => (T)GetVariable(name);

        public object GetVariable(string name)
        {
            var scope = Scopes.FirstOrDefault(x => x.Variables.ContainsKey(name)) ?? CurrentScope;
            return scope.GetVariable(name);
        }

        public Variables GetVariables() => Scopes
            .Reverse()
            .Select(x => x.Variables)
            .Aggregate(Variables.Empty, (x, y) => new Variables(x.Union(y)));

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => 
            ExpressionEvaluator.EvaluateAsync(expression, this, activityExecutionContext, cancellationToken);

        public Task<object> EvaluateAsync(IWorkflowExpression expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) =>
            ExpressionEvaluator.EvaluateAsync(expression, this, activityExecutionContext, cancellationToken);

        public void Suspend()
        {
            Status = WorkflowStatus.Suspended;
        }

        public void Fault(IActivity activity, LocalizedString message)
        {
            Status = WorkflowStatus.Faulted;
        }
    }
}