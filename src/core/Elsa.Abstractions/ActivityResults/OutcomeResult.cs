using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string>? outcomes = default)
        {
            var outcomeList = outcomes?.ToList() ?? new List<string>(1);

            if (!outcomeList.Any())
                outcomeList.Add(OutcomeNames.Done);

            Outcomes = outcomeList;
        }

        public IReadOnlyCollection<string> Outcomes { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            activityExecutionContext.Outcomes = Outcomes.ToList();

            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var nextActivities = GetNextActivities(
                workflowExecutionContext,
                activityExecutionContext.ActivityBlueprint.Id,
                Outcomes).ToList();

            workflowExecutionContext.ScheduleActivities(nextActivities, activityExecutionContext.Output);
        }

        private IEnumerable<string> GetNextActivities(
            WorkflowExecutionContext workflowContext,
            string sourceId,
            IEnumerable<string> outcomes)
        {
            var query =
                from connection in workflowContext.WorkflowBlueprint.Connections
                from outcome in outcomes
                let connectionOutcome = connection.Source.Outcome ?? OutcomeNames.Done
                let isConnectionOutcome = connectionOutcome.Equals(outcome, StringComparison.OrdinalIgnoreCase)
                where connection.Source.Activity.Id == sourceId && isConnectionOutcome
                from activityBlueprint in workflowContext.WorkflowBlueprint.Activities
                where activityBlueprint.Id == connection.Target.Activity.Id
                select activityBlueprint.Id;

            return query.Distinct();
        }
    }
}