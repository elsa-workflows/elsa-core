using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Results
{
    public class ScheduleActivitiesResult : IActivityExecutionResult
    {
        public ScheduleActivitiesResult(IEnumerable<IActivity> activities, Variable? input = default)
        {
            Activities = activities.Select(x => new ScheduledActivity(x, input));
        }
        
        public ScheduleActivitiesResult(IEnumerable<ScheduledActivity> activities)
        {
            Activities = activities;
        }
        
        public IEnumerable<ScheduledActivity> Activities { get; }

        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.ScheduleActivities(Activities);
            return Task.CompletedTask;
        }
    }
}