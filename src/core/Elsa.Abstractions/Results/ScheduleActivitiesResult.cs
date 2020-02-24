using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Results
{
    public class ScheduleActivitiesResult : ActivityExecutionResult
    {
        public ScheduleActivitiesResult(IEnumerable<IActivity> activities, Variable? input = default) => 
            Activities = activities.Select(x => new ScheduledActivity(x, input));

        public ScheduleActivitiesResult(IEnumerable<ScheduledActivity> activities) => Activities = activities;

        public IEnumerable<ScheduledActivity> Activities { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext) => 
            activityExecutionContext.WorkflowExecutionContext.ScheduleActivities(Activities);
    }
}