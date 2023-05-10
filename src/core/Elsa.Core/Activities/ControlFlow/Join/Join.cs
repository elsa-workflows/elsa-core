using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Merge workflow execution back into a single branch.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Join : Activity, INotificationHandler<WorkflowExecutionPassCompleted>
    {
        private readonly IMediator _mediator;

        public Join(IMediator mediator)
        {
            _mediator = mediator;
            InboundTransitions = new List<string>().AsReadOnly();
        }

        public enum JoinMode
        {
            WaitAll,
            WaitAny
        }
        
        [ActivityInput(Hint = "True if all blocking activities within the fork should be cleared.", UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool EagerJoin { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "WaitAll: wait for all incoming activities to have executed. WaitAny: continue execution as soon as any of the incoming activity has executed.",
            Options = new[] { "WaitAll", "WaitAny" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public JoinMode Mode { get; set; }

        public IReadOnlyCollection<string> InboundTransitions
        {
            get => GetState<IReadOnlyCollection<string>>(() => new List<string>());
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var isDone = IsDone(context);
            
            context.JournalData.Add("Completed", isDone);
            context.JournalData.Add("Current Inbound Transitions", InboundTransitions);

            if (context.WorkflowInstance.Faults.Count > 0)
            {
                context.JournalData.Add("Current Faulted Transitions", context.WorkflowInstance.Faults.Select(x => x.FaultedActivityId));

            }
            if (!isDone)
                return Noop();
            
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var ancestorActivityIds = workflowExecutionContext.GetInboundActivityPath(Id).ToList();
            var activities = workflowExecutionContext.WorkflowBlueprint.Activities.ToDictionary(x => x.Id);
            var ancestors = ancestorActivityIds.Select(x => activities[x]).ToList();
            var owningFork = ancestors.FirstOrDefault(x => x.Type == nameof(Fork));

            await RemoveBlockingActivitiesAsync(workflowExecutionContext, owningFork);
            await RemoveScopeActivitiesAsync(workflowExecutionContext, ancestors, owningFork);
            RemoveScheduledActivitiesAsync(workflowExecutionContext, owningFork);

            // Clear the recorded inbound transitions. This is necessary in case we're in a looping construct. 
            InboundTransitions = new List<string>();
            return Done();
        }

        private bool IsDone(ActivityExecutionContext context)
        {
            var recordedInboundTransitions = InboundTransitions;
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var inboundConnections = workflowExecutionContext.GetInboundConnections(Id);

            return Mode switch
            {
                JoinMode.WaitAll => inboundConnections.All(x => recordedInboundTransitions.Contains(GetTransitionKey(x)) &&
                        (
                        !context.WorkflowInstance.Faults.Contains(f => f.FaultedActivityId == GetActivityId(x)) &&
                        !context.WorkflowInstance.ScheduledActivities.Contains(f => f.ActivityId == GetActivityId(x))
                        )
                    ),
                JoinMode.WaitAny => inboundConnections.Any(x => recordedInboundTransitions.Contains(GetTransitionKey(x)) &&
                        (
                        !context.WorkflowInstance.Faults.Contains(f => f.FaultedActivityId == GetActivityId(x)) &&
                        !context.WorkflowInstance.ScheduledActivities.Contains(f => f.ActivityId == GetActivityId(x))
                        )
                    ),
                _ => false
            };
        }

        private async Task RemoveBlockingActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? fork)
        {
            var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.ToList();
            var inboundActivities = workflowExecutionContext.GetInboundActivityPath(Id).ToHashSet();

            // Remove all blocking activities between the fork and this join activity. 
            foreach (var blockingActivity in blockingActivities)
            {
                var blockingActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.GetActivity(blockingActivity.ActivityId)!;
                var blockingActivityAncestors = workflowExecutionContext.GetInboundActivityPath(blockingActivity.ActivityId).ToList();

                // Include composite activities in the equation.
                if (blockingActivityBlueprint.Parent != null)
                {
                    var compositeBlockingActivityAncestors = workflowExecutionContext.GetInboundActivityPath(blockingActivityBlueprint.Parent.Id).ToList();
                    blockingActivityAncestors = blockingActivityAncestors.Concat(compositeBlockingActivityAncestors).ToList();
                }

                if (EagerJoin)
                {
                    if (fork == null || blockingActivityAncestors.Contains(fork.Id))
                        await workflowExecutionContext.RemoveBlockingActivityAsync(blockingActivity);
                }
                else
                {
                    // If the fork is inbound in the blocking activity AND the blocking activity is inbound in this Join, then clear it.
                    var blockingActivityHasInboundFork = fork == null || blockingActivityAncestors.Contains(fork.Id);
                    var joinActivityHasInboundBlockingActivity = inboundActivities.Contains(blockingActivity.ActivityId);
                
                    if (blockingActivityHasInboundFork && joinActivityHasInboundBlockingActivity)
                        await workflowExecutionContext.RemoveBlockingActivityAsync(blockingActivity);
                }
            }
        }
        
        private void RemoveScheduledActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? fork)
        {
            if (!EagerJoin) return;
            
            var scheduledActivities = workflowExecutionContext.WorkflowInstance.ScheduledActivities.ToList();
            
            foreach (var scheduledActivity in scheduledActivities)
            {
                var blockingActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.GetActivity(scheduledActivity.ActivityId)!;
                var blockingActivityAncestors = workflowExecutionContext.GetInboundActivityPath(scheduledActivity.ActivityId).ToList();

                // Include composite activities in the equation.
                if (blockingActivityBlueprint.Parent != null)
                {
                    var compositeBlockingActivityAncestors = workflowExecutionContext.GetInboundActivityPath(blockingActivityBlueprint.Parent.Id).ToList();
                    blockingActivityAncestors = blockingActivityAncestors.Concat(compositeBlockingActivityAncestors).ToList();
                }
                
                if (fork == null || blockingActivityAncestors.Contains(fork.Id))
                    workflowExecutionContext.WorkflowInstance.ScheduledActivities.Remove(scheduledActivity);
            }
        }

        private async Task RemoveScopeActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, ICollection<IActivityBlueprint> ancestors, IEnumerable<IActivityBlueprint> forks)
        {
            foreach (var fork in forks) 
                await RemoveScopeActivitiesAsync(workflowExecutionContext, ancestors, fork);
        }

        private async Task RemoveScopeActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, ICollection<IActivityBlueprint> ancestors, IActivityBlueprint? fork)
        {
            var scopes = workflowExecutionContext.WorkflowInstance.Scopes.AsEnumerable().Reverse().ToList();

            // Take only ancestors up until the specified fork (if any).
            if (fork != null) ancestors = ancestors.TakeUntil(x => x.Id == fork.Id).ToList();

            for (var i = 0; i < scopes.Count; i++)
            {
                var activityScope = scopes.ElementAt(i);
                var scopeActivityBlueprint = workflowExecutionContext.WorkflowBlueprint.GetActivity(activityScope.ActivityId)!;
                var scopeActivityAncestors = workflowExecutionContext.GetInboundActivityPath(activityScope.ActivityId);

                // Include composite activities in the equation.
                if (scopeActivityBlueprint.Parent != null)
                {
                    var compositeScopeActivityAncestors = workflowExecutionContext.GetInboundActivityPath(scopeActivityBlueprint.Parent.Id).ToList();
                    scopeActivityAncestors = scopeActivityAncestors.Concat(compositeScopeActivityAncestors).ToList();
                }

                if (ancestors.All(x => x.Id != activityScope.ActivityId))
                    continue;

                if (fork != null && !scopeActivityAncestors.Contains(fork.Id))
                    continue;

                var evictedScopes = scopes.Skip(i).ToList();
                scopes = scopes.Take(i).ToList();

                foreach (var evictedScope in evictedScopes)
                {
                    var activity = workflowExecutionContext.WorkflowBlueprint.GetActivity(evictedScope.ActivityId)!;
                    await _mediator.Publish(new ScopeEvicted(workflowExecutionContext, activity));
                }

                workflowExecutionContext.WorkflowInstance.Scopes = new SimpleStack<ActivityScope>(scopes.AsEnumerable().Reverse());
                break;
            }
        }

        private void RecordInboundTransitionsAsync(ActivityExecutionContext activityExecutionContext)
        {
            var activityId = activityExecutionContext.ActivityBlueprint.Id;

            // Only record activity execution if the executed activity isn't blocking.
            if(activityExecutionContext.WorkflowInstance.BlockingActivities.Any(x => x.ActivityId == activityId))
                return;
            
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;

            // Get outbound connections of the executing activity.
            var outboundConnections = workflowExecutionContext.GetOutboundConnections(activityId);

            // Get any connection that is pointing to this activity.
            var inboundTransitionsQuery =
                from connection in outboundConnections
                let targetActivity = connection.Target.Activity
                where targetActivity.Type == nameof(Join)
                select connection;

            foreach (var inboundConnections in inboundTransitionsQuery.GroupBy(conn => conn.Target.Activity.Id))
            {
                var joinBlueprint = inboundConnections.First().Target.Activity;
                var joinActivityData = joinBlueprint != null ? workflowExecutionContext.WorkflowInstance.ActivityData.GetItem(joinBlueprint.Id, () => new Dictionary<string, object?>()) : default;

                if (joinActivityData != null)
                {
                    var inboundTransitions = joinActivityData.GetState<IReadOnlyCollection<string>?>(nameof(InboundTransitions)) ?? new List<string>();

                    // For each inbound connection, record the transition.
                    foreach (var inboundConnection in inboundConnections)
                    {
                        inboundTransitions = inboundTransitions
                            .Union(new[] { GetTransitionKey(inboundConnection) })
                            .Distinct()
                            .ToList();
                    }

                    joinActivityData.SetState(nameof(InboundTransitions), inboundTransitions);
                }
            }
        }

        private string GetTransitionKey(IConnection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Outcome;

            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }
        private string GetActivityId(IConnection connection)
        {
            return connection.Source.Activity.Id;
        }

        public Task Handle(WorkflowExecutionPassCompleted notification, CancellationToken cancellationToken)
        {
            RecordInboundTransitionsAsync(notification.ActivityExecutionContext);
            return Task.CompletedTask;
        }
    }
}