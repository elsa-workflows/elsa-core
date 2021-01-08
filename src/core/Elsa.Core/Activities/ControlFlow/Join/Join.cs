using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Events;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Merge workflow execution back into a single branch.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Join : Activity, INotificationHandler<ActivityExecuted>
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

        [ActivityProperty(Type = ActivityPropertyTypes.Select, Hint = "Either 'WaitAll' or 'WaitAny'")]
        [SelectOptions("WaitAll", "WaitAny")]
        public JoinMode Mode { get; set; }

        public IReadOnlyCollection<string> InboundTransitions
        {
            get => GetState<IReadOnlyCollection<string>>(() => new List<string>());
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var recordedInboundTransitions = InboundTransitions;
            var inboundConnections = workflowExecutionContext.GetInboundConnections(Id);

            var done = Mode switch
            {
                JoinMode.WaitAll => inboundConnections.All(x => recordedInboundTransitions.Contains(GetTransitionKey(x))),
                JoinMode.WaitAny => inboundConnections.Any(x => recordedInboundTransitions.Contains(GetTransitionKey(x))),
                _ => false
            };

            if (done)
            {
                // Remove any blocking activities within the first fork.
                var ancestorActivityIds = workflowExecutionContext.GetInboundActivityPath(Id).ToList();
                var ancestors = workflowExecutionContext.WorkflowBlueprint.Activities.Where(x => ancestorActivityIds.Contains(x.Id)).ToList();
                var fork = ancestors.FirstOrDefault(x => x.Type == nameof(Fork));
                var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.ToList();

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

                    if (fork == null || blockingActivityAncestors.Contains(fork.Id))
                    {
                        workflowExecutionContext.WorkflowInstance.BlockingActivities.Remove(blockingActivity);
                        await _mediator.Publish(new BlockingActivityRemoved(workflowExecutionContext, blockingActivity));
                    }
                }
            }

            if (!done)
                return Noop();

            // Clear the recorded inbound transitions. This is necessary in case we're in a looping construct. 
            InboundTransitions = new List<string>();
            return Done();
        }

        private void RecordInboundTransitionsAsync(ActivityExecutionContext activityExecutionContext)
        {
            var activityId = activityExecutionContext.ActivityBlueprint.Id;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;

            // Get outbound connections of the executing activity.
            var outboundConnections = workflowExecutionContext.GetOutboundConnections(activityId);

            // Get any connection that is pointing to this activity.
            var inboundTransitionsQuery =
                from connection in outboundConnections
                let targetActivity = connection.Target.Activity
                where targetActivity.Type == nameof(Join)
                select connection;

            var inboundConnections = inboundTransitionsQuery.ToList();
            var joinBlueprint = inboundConnections.FirstOrDefault()?.Target.Activity;
            var joinActivityData = joinBlueprint != null ? workflowExecutionContext.WorkflowInstance.ActivityData.GetItem(joinBlueprint.Id, () => new JObject()) : default;

            if (joinActivityData == null)
                return;
            
            var inboundTransitions = joinActivityData.GetState<IReadOnlyCollection<string>?>(nameof(InboundTransitions)) ?? new List<string>();

            // For each inbound connection, record the transition.
            foreach (var inboundConnection in inboundConnections)
            {
                inboundTransitions = inboundTransitions
                    .Union(new[] { GetTransitionKey(inboundConnection) })
                    .Distinct()
                    .ToList();
            }

            joinActivityData.SetState(nameof(inboundTransitions), inboundTransitions);
        }

        private string GetTransitionKey(IConnection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Outcome;

            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            RecordInboundTransitionsAsync(notification.ActivityExecutionContext);
            return Task.CompletedTask;
        }
    }
}