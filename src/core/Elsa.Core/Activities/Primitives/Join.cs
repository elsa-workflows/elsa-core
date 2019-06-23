using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Core.Activities.Primitives
{
    public class Join : Activity, IWorkflowEventHandler
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
            var recordedInboundTransitions = InboundTransitions ?? new List<string>();
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

                    if (done)
                    {
                        // Remove any inbound blocking activities.
                        var ancestorActivityIds = workflow.GetInboundActivityPath(Id).ToList();
                        var blockingActivities = workflow.BlockingActivities.Where(x => ancestorActivityIds.Contains(x.Id)).ToList();

                        foreach (var blockingActivity in blockingActivities)
                        {
                            workflow.BlockingActivities.Remove(blockingActivity);
                        }
                    }

                    break;
            }

            return done ? Done() : Noop();
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
                where destinationActivity.TypeName == nameof(Join)
                select connection;

            var inboundConnections = inboundTransitionsQuery.ToList();

            // For each inbound connection, record the transition.
            foreach (var inboundConnection in inboundConnections)
            {
                var joinActivity = (Join) inboundConnection.Target.Activity;
                var inboundTransitions = joinActivity.InboundTransitions ?? new List<string>();
                joinActivity.InboundTransitions = inboundTransitions.Union(new[] { GetTransitionKey(inboundConnection) }).Distinct().ToList();
            }
        }

        private string GetTransitionKey(Connection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Outcome;

            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }

        Task IWorkflowEventHandler.ActivityExecutedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken)
        {
            RecordInboundTransitions(workflowContext, activity);

            return Task.CompletedTask;
        }

        Task IWorkflowEventHandler.InvokingHaltedActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken) => Task.CompletedTask;
        Task IWorkflowEventHandler.WorkflowInvokedAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}