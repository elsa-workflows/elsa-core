using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string>? outcomes = default, Variable? output = default)
        {
            var outcomeList = outcomes?.ToList() ?? new List<string>(1);

            if (!outcomeList.Any()) 
                outcomeList.Add(OutcomeNames.Done);

            Outcomes = outcomeList;
            Output = output;
        }

        public IReadOnlyCollection<string> Outcomes { get; }
        public Variable? Output { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            if(Output != null)
                activityExecutionContext.Output = Output;
            
            activityExecutionContext.Outcomes = Outcomes.ToList();

            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var nextActivities = GetNextActivities(workflowExecutionContext, activityExecutionContext.Activity, Outcomes).ToList();
            
            workflowExecutionContext.ScheduleActivities(nextActivities, Output);
        }
        
        private IEnumerable<IActivity> GetNextActivities(WorkflowExecutionContext workflowContext, IActivity source, IEnumerable<string> outcomes)
        {
            var query =
                from connection in workflowContext.Connections
                from outcome in outcomes
                where connection.Source.Activity == source && (connection.Source.Outcome ?? OutcomeNames.Done).Equals(outcome, StringComparison.OrdinalIgnoreCase)
                select connection.Target.Activity;

            return query.Distinct();
        }
    }
}