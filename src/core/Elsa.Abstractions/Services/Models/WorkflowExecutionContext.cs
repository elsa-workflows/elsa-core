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
        public WorkflowExecutionContext(
            IExpressionEvaluator expressionEvaluator, 
            IServiceProvider serviceProvider,
            string definitionId,
            string instanceId, 
            int version,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            string correlationId = default,
            Variables variables = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted,
            WorkflowFault? workflowFault = default,
            IEnumerable<ExecutionLogEntry> executionLog = default)
        {
            ServiceProvider = serviceProvider;
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Version = version;
            CorrelationId = correlationId;
            Activities = activities.ToList();
            Connections = connections.ToList();
            ExpressionEvaluator = expressionEvaluator;
            ScheduledActivities = scheduledActivities != null ? new Stack<ScheduledActivity>(scheduledActivities) : new Stack<ScheduledActivity>();
            BlockingActivities = blockingActivities != null ? new HashSet<IActivity>(blockingActivities) : new HashSet<IActivity>();
            Variables = variables ?? new Variables();
            Status = status;
            PersistenceBehavior = persistenceBehavior;
            WorkflowFault = workflowFault;
            ExecutionLog = executionLog?.ToList() ?? new List<ExecutionLogEntry>();
        }

        public IServiceProvider ServiceProvider { get; }
        public string DefinitionId { get; }
        public ICollection<IActivity> Activities { get; }
        public ICollection<Connection> Connections { get; }
        public WorkflowStatus Status { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; }
        public HashSet<IActivity> BlockingActivities { get; }
        public Variables Variables { get; }
        public bool HasScheduledActivities => ScheduledActivities.Any();
        public ScheduledActivity? ScheduledActivity { get; private set; }
        public WorkflowFault? WorkflowFault { get; private set; }
        public Variable? Output { get; set; }

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
        public void ScheduleActivity(IActivity activity, Variable? input = default) => ScheduleActivity(new ScheduledActivity(activity, input));
        public void ScheduleActivity(ScheduledActivity activity) => ScheduledActivities.Push(activity);

        public ScheduledActivity PopScheduledActivity() => ScheduledActivity = ScheduledActivities.Pop();
        public ScheduledActivity PeekScheduledActivity() => ScheduledActivities.Peek();
        public IExpressionEvaluator ExpressionEvaluator { get; }
        public string InstanceId { get; set; }
        public int Version { get; }
        public string CorrelationId { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; }

        public bool AddBlockingActivity(IActivity activity) => BlockingActivities.Add(activity);
        public void SetVariable(string name, object value) => Variables.SetVariable(name, value);
        public T GetVariable<T>(string name) => (T)GetVariable(name);
        public object GetVariable(string name) => Variables.GetVariable(name);

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => 
            ExpressionEvaluator.EvaluateAsync(expression, activityExecutionContext, cancellationToken);

        public Task<object> EvaluateAsync(IWorkflowExpression expression, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) =>
            ExpressionEvaluator.EvaluateAsync(expression, activityExecutionContext, cancellationToken);

        public void Suspend()
        {
            Status = WorkflowStatus.Suspended;
        }

        public void Fault(IActivity? activity, LocalizedString? message)
        {
            Status = WorkflowStatus.Faulted;
            WorkflowFault = new WorkflowFault(activity, message);
        }

        public void Complete()
        {
            Status = WorkflowStatus.Completed;
        }
    }
}