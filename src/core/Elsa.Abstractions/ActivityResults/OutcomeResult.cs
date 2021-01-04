﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string>? outcomes = default, string? parentId = default)
        {
            ParentId = parentId;
            var outcomeList = outcomes?.ToList() ?? new List<string>(1);

            if (!outcomeList.Any())
                outcomeList.Add(OutcomeNames.Done);

            Outcomes = outcomeList;
        }

        public IEnumerable<string> Outcomes { get; }
        public string? ParentId { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var outcomes = activityExecutionContext.Outcomes = Outcomes.ToList();
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;

            var nextActivities = GetNextActivities(
                workflowExecutionContext,
                activityExecutionContext.ActivityBlueprint.Id,
                outcomes).ToList();

            workflowExecutionContext.ScheduleActivities(nextActivities,  activityExecutionContext.Output);
        }

        private IEnumerable<string> GetNextActivities(
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
                from activityBlueprint in workflowContext.WorkflowBlueprint.Activities
                where activityBlueprint.Id == connection.Target.Activity.Id
                orderby outcome.order 
                select activityBlueprint.Id;

            return query.Distinct();
        }
    }
}