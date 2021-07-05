using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string>? outcomes = default, object? input = null, string? parentId = default)
        {
            Input = input;
            ParentId = parentId;
            var outcomeList = outcomes?.ToList() ?? new List<string>(1);

            if (!outcomeList.Any())
                outcomeList.Add(OutcomeNames.Done);

            Outcomes = outcomeList;
        }

        public IEnumerable<string> Outcomes { get; }
        public object? Input { get; }
        public string? ParentId { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var outcomes = activityExecutionContext.Outcomes = Outcomes.ToList();
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var nextConnections = GetNextConnections(workflowExecutionContext, activityExecutionContext.ActivityBlueprint.Id, outcomes).ToList();

            // See if we got a "default" connection (from the current activity to the next activity via the default "Done" outcome).
            // Unless the current activity is a "scope" activity, in which case it will schedule a Done outcome itself.
            // TODO: This is another sign that we need to refactor activity scopes into a natural contained activity structure.
            if (!outcomes.Contains(OutcomeNames.Done) && !nextConnections.Any() && activityExecutionContext.ActivityId != activityExecutionContext.CurrentScope?.ActivityId)
                nextConnections = GetNextConnections(workflowExecutionContext, activityExecutionContext.ActivityId, new[] { OutcomeNames.Done }).ToList();
            
            var nextActivities =
                (
                    from connection in nextConnections
                    from activityBlueprint in workflowExecutionContext.WorkflowBlueprint.Activities
                    where activityBlueprint.Id == connection.Target.Activity.Id
                    select activityBlueprint.Id
                )
                .Distinct();

            foreach (var nextConnection in nextConnections)
                workflowExecutionContext.ExecutionLog.Add(nextConnection);

            workflowExecutionContext.ScheduleActivities(nextActivities, Input);
        }

        public static IEnumerable<string> GetNextActivities(
            WorkflowExecutionContext workflowExecutionContext,
            string sourceId,
            IEnumerable<string> outcomes)
        {
            var nextConnections = GetNextConnections(workflowExecutionContext, sourceId, outcomes);

            var query =
                from connection in nextConnections
                from activityBlueprint in workflowExecutionContext.WorkflowBlueprint.Activities
                where activityBlueprint.Id == connection.Target.Activity.Id
                select activityBlueprint.Id;

            return query.Distinct();
        }

        public static IEnumerable<IConnection> GetNextConnections(
            WorkflowExecutionContext workflowContext,
            string sourceId,
            IEnumerable<string> outcomes)
        {
            var orderedOutcomes = outcomes.Select((outcome, order) => (outcome, order));

            var query =
                from connection in workflowContext.WorkflowBlueprint.Connections
                from outcome in orderedOutcomes
                let connectionOutcome = connection.Source.Outcome ?? OutcomeNames.Done
                let isConnectionOutcome = connectionOutcome.Equals(outcome.outcome, StringComparison.OrdinalIgnoreCase)
                where connection.Source.Activity.Id == sourceId && isConnectionOutcome
                orderby outcome.order
                select connection;

            return query;
        }
    }
}