using System.Linq;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    /// <summary>
    /// Schedules the previously executed activity, effectively going back one step.
    /// </summary>
    public class GoBackResult : ActivityExecutionResult
    {
        private readonly int _steps;
        private readonly object? _input;

        public GoBackResult(object? input = default, int steps = 1)
        {
            _input = input;
            _steps = steps;
        }
        
        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var previousEntry = workflowExecutionContext.ExecutionLog.Skip(_steps).FirstOrDefault();

            if (previousEntry == null)
                return;

            var activityId = previousEntry.Source.Activity.Id;
            workflowExecutionContext.ScheduleActivity(activityId, _input);
        }
    }
}