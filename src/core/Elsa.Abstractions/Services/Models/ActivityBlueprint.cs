using System;
using Elsa.ActivityResults;

namespace Elsa.Services.Models
{
    public class ActivityBlueprint : IActivityBlueprint
    {
        public ActivityBlueprint()
        {
        }
        
        public ActivityBlueprint(Func<ActivityExecutionContext, IActivity> createActivity)
        {
            CreateActivity = createActivity;
        }
        
        public ActivityBlueprint(string id, Func<ActivityExecutionContext, IActivity> createActivity)
        {
            Id = id;
            CreateActivity = createActivity;
        }

        public string Id { get; set; } = default!;
        public Func<ActivityExecutionContext, IActivity> CreateActivity { get; set; } = default!;
    }
}