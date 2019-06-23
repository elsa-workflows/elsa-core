using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Services
{
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IActivityResolver activityResolver;

        public WorkflowFactory(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }
        
        public Workflow CreateWorkflow(WorkflowBlueprint blueprint, Variables input = null, WorkflowInstance workflowInstance = null)
        {
            var activities = CreateActivities(blueprint.Activities);
            return new Workflow(blueprint.Id, activities, input, workflowInstance);
        }

        private IEnumerable<IActivity> CreateActivities(IEnumerable<ActivityBlueprint> activityBlueprints)
        {
            return activityBlueprints.Select(CreateActivity);
        }

        private IActivity CreateActivity(ActivityBlueprint blueprint)
        {
            var activity = activityResolver.ResolveActivity(blueprint.TypeName);

            activity.State = new JObject(blueprint.State);
            activity.Id = blueprint.Id;
            
            return activity;
        }
    }
}