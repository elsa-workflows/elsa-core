using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
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
            InboundTransitions = new List<string>().AsReadOnly();
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

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            // var recordedInboundTransitions = InboundTransitions ?? new List<string>();
            // var processInstance = context.ProcessExecutionContext.ProcessInstance;
            // var inboundConnections = processInstance.GetInboundConnections(Id);
            // var done = false;
            //
            // switch (Mode)
            // {
            //     case JoinMode.WaitAll:
            //         done = inboundConnections.All(x => recordedInboundTransitions.Contains(GetTransitionKey(x)));
            //         break;
            //     case JoinMode.WaitAny:
            //         done = inboundConnections.Any(x => recordedInboundTransitions.Contains(GetTransitionKey(x)));
            //         break;
            // }
            //
            // if (done)
            // {
            //     // Remove any inbound blocking activities.
            //     var ancestorActivityIds = processInstance.GetInboundActivityPath(Id).ToList();
            //     var blockingActivities =
            //         processInstance.BlockingActivities.Where(x => ancestorActivityIds.Contains(x.Id)).ToList();
            //
            //     foreach (var blockingActivity in blockingActivities)
            //     {
            //         processInstance.BlockingActivities.Remove(blockingActivity);
            //     }
            // }
            //
            // if (!done)
            //     return Noop();
            //
            // // Clear the recorded inbound transitions. This is necessary in case we're in a looping construct. 
            // InboundTransitions = new List<string>();
            return Done();
        }

        private void RecordInboundTransitions(ProcessExecutionContext processContext, IActivity activity)
        {
            // var process = processContext.ProcessInstance;
            //
            // // Get outbound connections of the executing activity.
            // var outboundConnections = process.GetOutboundConnections(activity.Id);
            //
            // // Get any connection that is pointing to this activity.
            // var inboundTransitionsQuery =
            //     from connection in outboundConnections
            //     let targetActivity = connection.Target.Activity
            //     where targetActivity.Type == nameof(Join)
            //     select connection;
            //
            // var inboundConnections = inboundTransitionsQuery.ToList();
            //
            // // For each inbound connection, record the transition.
            // foreach (var inboundConnection in inboundConnections)
            // {
            //     var joinActivity = (Join)inboundConnection.Target.Activity;
            //     var inboundTransitions = joinActivity.InboundTransitions ?? new List<string>();
            //     joinActivity.InboundTransitions = inboundTransitions
            //         .Union(new[] { GetTransitionKey(inboundConnection) })
            //         .Distinct()
            //         .ToList();
            // }
        }

        // private string GetTransitionKey(Connection connection)
        // {
        //     var sourceActivityId = connection.Source.Activity.Id;
        //     var sourceOutcomeName = connection.Source.Outcome;
        //
        //     return $"@{sourceActivityId}_{sourceOutcomeName}";
        // }

        Task IWorkflowEventHandler.ActivityExecutedAsync(
            ProcessExecutionContext processContext,
            IActivity activity,
            CancellationToken cancellationToken)
        {
            RecordInboundTransitions(processContext, activity);

            return Task.CompletedTask;
        }

        public Task ActivityFaultedAsync(
            ProcessExecutionContext processExecutionContext,
            IActivity activity,
            string message,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task IWorkflowEventHandler.InvokingHaltedActivitiesAsync(
            ProcessExecutionContext processExecutionContext,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task IWorkflowEventHandler.WorkflowInvokedAsync(
            ProcessExecutionContext processExecutionContext,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}