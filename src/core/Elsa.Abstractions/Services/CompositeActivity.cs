using System.Collections.Generic;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class CompositeActivity : Activity
    {
        public abstract void Build(IWorkflowBuilder workflowBuilder);

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var startActivities = context.ActivityBlueprint.ChildWorkflow!.GetStartActivities().Select(x => x.Id).ToList();
            return Combine(Done(), Schedule(startActivities, null!));
        }
    }
}