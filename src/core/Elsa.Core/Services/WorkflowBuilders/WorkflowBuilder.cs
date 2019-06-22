using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class WorkflowBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IList<ActivityBuilder> activityBuilders = new List<ActivityBuilder>();

        public WorkflowBuilder(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }

        public ActivityBuilder<T> Add<T>(Action<T> configureActivity) where T : IActivity
        {
            var activity = activityResolver.ResolveActivity(configureActivity);
            var activityBuilder = new ActivityBuilder<T>(this, activity);

            activityBuilders.Add(activityBuilder);
            return activityBuilder;
        }

        public Workflow Build()
        {
            var activities = activityBuilders.Select(x => x.Activity);
            var connections = activityBuilders.SelectMany(x => x.Connections).Select(x => x.BuildConnection());
            
            return new Workflow(activities, connections, Variables.Empty);
        }
    }
}