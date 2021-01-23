using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Merge workflow execution back into a single branch.",
        Icon = "fas fa-code-branch",
        RuntimeDescription = "x => !!x.state.joinMode ? `Merge workflow execution back into a single branch using mode <strong>${ x.state.joinMode }</strong>` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Join : Activity, IWorkflowEventHandler
    {
        public Join()
        {
            InboundTransitions = new List<string>();
        }

        public enum JoinMode
        {
            WaitAll,
            WaitAny
        }

        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "Either 'WaitAll' or 'WaitAny'")
        ]
        [SelectOptions("WaitAll", "WaitAny")]
        public JoinMode Mode
        {
            get => GetState(() => JoinMode.WaitAll);
            set => SetState(value);
        }

        public IReadOnlyCollection<string> InboundTransitions
        {
            get => GetState<IReadOnlyCollection<string>>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            var recordedInboundTransitions = InboundTransitions ?? Enumerable.Empty<string>();
            var workflow = workflowContext.Workflow;
            var inboundConnections = workflow.GetInboundConnections(Id);
            var done = false;

            switch (Mode)
            {
                case JoinMode.WaitAll:
                    done = inboundConnections.All(x => recordedInboundTransitions.Contains(GetTransitionKey(x)));
                    break;
                case JoinMode.WaitAny:
                    done = inboundConnections.Any(x => recordedInboundTransitions.Contains(GetTransitionKey(x)));
                    break;
            }

            if (done)
            {
                // Remove any inbound blocking activities.
                var ancestorActivityIds = workflow.GetInboundActivityPath(Id).ToList();
                var blockingActivities =
                    workflow.BlockingActivities.Where(x => ancestorActivityIds.Contains(x.Id)).ToList();

                foreach (var blockingActivity in blockingActivities)
                {
                    workflow.BlockingActivities.Remove(blockingActivity);
                }
            }

            if (!done)
                return Noop();
            
            // Clear the recorded inbound transitions. This is necessary in case we're in a looping construct. 
            InboundTransitions = new List<string>();
            return Done();
        }

        private void RecordInboundTransitions(WorkflowExecutionContext workflowContext, IActivity activity)
        {
            var workflow = workflowContext.Workflow;

            // Get outbound connections of the executing activity.
            var outboundConnections = workflow.GetOutboundConnections(activity.Id);

            // Get any connection that is pointing to this activity.
            var inboundTransitionsQuery =
                from connection in outboundConnections
                let destinationActivity = connection.Target.Activity
                where destinationActivity is Join
                select connection;

            var inboundConnections = inboundTransitionsQuery.ToList();

            // For each inbound connection, record the transition.
            foreach (var inboundConnection in inboundConnections)
            {
                var joinActivity = (Join)inboundConnection.Target.Activity;
                var inboundTransitions = joinActivity.InboundTransitions ?? new List<string>();
                joinActivity.InboundTransitions = inboundTransitions
                    .Union(new[] { GetTransitionKey(inboundConnection) })
                    .Distinct()
                    .ToList();
            }
        }

        private string GetTransitionKey(Connection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Outcome;

            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }

        public Task ExecutingActivityAsync(
            WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task IWorkflowEventHandler.ActivityExecutedAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            CancellationToken cancellationToken)
        {
            RecordInboundTransitions(workflowContext, activity);

            return Task.CompletedTask;
        }

        public Task ActivityFaultedAsync(
            WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            string message,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task IWorkflowEventHandler.InvokingHaltedActivitiesAsync(
            WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task IWorkflowEventHandler.WorkflowInvokedAsync(
            WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}