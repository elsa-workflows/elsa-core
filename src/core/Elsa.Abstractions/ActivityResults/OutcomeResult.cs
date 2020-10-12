using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string>? outcomes = default, object? output = default)
        {
            var outcomeList = outcomes?.ToList() ?? new List<string>(1);

            if (!outcomeList.Any())
                outcomeList.Add(OutcomeNames.Done);

            Outcomes = outcomeList;
            Output = output;
        }

        public IReadOnlyCollection<string> Outcomes { get; }
        public object? Output { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            if (Output != null)
                activityExecutionContext.Output = Output;

            activityExecutionContext.Outcomes = Outcomes.ToList();

            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var nextActivities = GetNextActivities(
                workflowExecutionContext,
                activityExecutionContext.ActivityDefinition,
                Outcomes).ToList();

            workflowExecutionContext.ScheduleActivities(nextActivities, Output);
        }

        private IEnumerable<ActivityDefinition> GetNextActivities(
            WorkflowExecutionContext workflowContext,
            ActivityDefinition source,
            IEnumerable<string> outcomes)
        {
            var query =
                from connection in workflowContext.WorkflowDefinition.Connections
                from outcome in outcomes
                let connectionOutcome = connection.Outcome ?? OutcomeNames.Done
                let isConnectionOutcome = connectionOutcome.Equals(outcome, StringComparison.OrdinalIgnoreCase)
                where connection.SourceActivityId == source.Id && isConnectionOutcome
                from activityDefinition in workflowContext.WorkflowDefinition.Activities
                where activityDefinition.Id == connection.TargetActivityId
                select activityDefinition;

            return query.Distinct();
        }
    }
}