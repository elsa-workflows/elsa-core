using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class PostScheduleActivitiesResult : ActivityExecutionResult
    {
        public PostScheduleActivitiesResult(IEnumerable<string> activityIds, object? input = default) => 
            Activities = activityIds.Select(x => new ScheduledActivity(x, input));

        public PostScheduleActivitiesResult(IEnumerable<ScheduledActivity> activities) => Activities = activities;

        public IEnumerable<ScheduledActivity> Activities { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext) => 
            activityExecutionContext.WorkflowExecutionContext.PostScheduleActivities(Activities);
    }
}