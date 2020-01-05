using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Expressions;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using NodaTime;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContext
    {
        public WorkflowExecutionContext(
            IExpressionEvaluator expressionEvaluator, 
            IServiceProvider serviceProvider, 
            string instanceId, 
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            IEnumerable<WorkflowExecutionScope>? scopes = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted)
        {
            ServiceProvider = serviceProvider;
            InstanceId = instanceId;
            ExpressionEvaluator = expressionEvaluator;
            ScheduledActivities = scheduledActivities != null ? new Stack<ScheduledActivity>(scheduledActivities) : new Stack<ScheduledActivity>();
            BlockingActivities = blockingActivities != null ? new HashSet<IActivity>(blockingActivities) : new HashSet<IActivity>();
            Scopes = scopes != null ? new Stack<WorkflowExecutionScope>(scopes) : new Stack<WorkflowExecutionScope>();
            Status = status;
            PersistenceBehavior = persistenceBehavior;
        }

        public WorkflowStatus Status { get; set; }
        public IServiceProvider ServiceProvider { get; }
        public Stack<ScheduledActivity> ScheduledActivities { get; }
        public HashSet<IActivity> BlockingActivities { get; }
        public Stack<WorkflowExecutionScope> Scopes { get; private set; }
        public bool HasScheduledActivities => ScheduledActivities.Any();
        public WorkflowExecutionScope? CurrentScope => Scopes.Any() ?  Scopes.Peek() : default;
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

        public void ScheduleActivity(IActivity activity, object? input = default) => ScheduleActivity(new ScheduledActivity(activity, input));
        public void ScheduleActivity(ScheduledActivity activity) => ScheduledActivities.Push(activity);

        public ScheduledActivity PopScheduledActivity() => ScheduledActivity = ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => ScheduledActivities.Peek();
        public void BeginScope(IActivity? container = default)
        {
            if(CurrentScope?.Container != container)
                Scopes.Push(new WorkflowExecutionScope(container));
        }

        public void EndScope() => Scopes.Pop();
        public IExpressionEvaluator ExpressionEvaluator { get; }
        public string InstanceId { get; set; }
        public string CorrelationId { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }

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

        public void Complete()
        {
            Status = WorkflowStatus.Completed;
        }
    }
}