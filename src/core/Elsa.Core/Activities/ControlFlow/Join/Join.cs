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

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Merge workflow execution back into a single branch.",
        Icon = "fas fa-code-branch",
        RuntimeDescription = "x => !!x.state.joinMode ? `Merge workflow execution back into a single branch using mode <strong>${ x.state.joinMode }</strong>` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Join : Activity, INotificationHandler<ActivityExecuted>
    {
        public Join()
        {
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

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
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
                // Remove any inbound blocking activities.
                var ancestorActivityIds = workflowExecutionContext.GetInboundActivityPath(Id).ToList();
                var blockingActivities = workflowExecutionContext.WorkflowInstance.BlockingActivities.Where(x => ancestorActivityIds.Contains(x.ActivityId)).ToList();
            
                foreach (var blockingActivity in blockingActivities) 
                    workflowExecutionContext.WorkflowInstance.BlockingActivities.Remove(blockingActivity);
            }
            
            if (!done)
                return Noop();
            
            // Clear the recorded inbound transitions. This is necessary in case we're in a looping construct. 
            InboundTransitions = new List<string>();
            return Done();
        }

        private async Task RecordInboundTransitionsAsync(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint activity, CancellationToken cancellationToken)
        {
            // Get outbound connections of the executing activity.
            var outboundConnections = workflowExecutionContext.GetOutboundConnections(activity.Id);
            
            // Get any connection that is pointing to this activity.
            var inboundTransitionsQuery =
                from connection in outboundConnections
                let targetActivity = connection.Target.Activity
                where targetActivity.Type == nameof(Join)
                select connection;
            
            var inboundConnections = inboundTransitionsQuery.ToList();
            var joinBlueprint = inboundConnections.FirstOrDefault()?.Target.Activity;
            var joinActivity = joinBlueprint != null ? (Join)await joinBlueprint.CreateActivityAsync(new ActivityExecutionContext(workflowExecutionContext, workflowExecutionContext.ServiceProvider, joinBlueprint), cancellationToken) : default;
            
            // For each inbound connection, record the transition.
            foreach (var inboundConnection in inboundConnections)
            {
                var inboundTransitions = joinActivity!.InboundTransitions;
                
                joinActivity!.InboundTransitions = inboundTransitions
                    .Union(new[] { GetTransitionKey(inboundConnection) })
                    .Distinct()
                    .ToList();
            }
        }

        private string GetTransitionKey(IConnection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Outcome;
        
            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            await RecordInboundTransitionsAsync(notification.WorkflowExecutionContext, notification.Activity, cancellationToken);
        }
    }
}