using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.ActivityResults
{
    public class ScheduleActivitiesResult : ActivityExecutionResult
    {
        public ScheduleActivitiesResult(IEnumerable<IActivity> activities, object? input = default) => 
            Activities = activities.Select(x => new ScheduledActivity(x, input));

        public ScheduleActivitiesResult(IEnumerable<ScheduledActivity> activities) => Activities = activities;

        public IEnumerable<ScheduledActivity> Activities { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext) => 
            activityExecutionContext.WorkflowExecutionContext.ScheduleActivities(Activities);
    }
}