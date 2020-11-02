using System.Linq;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class CompositeActivity : Activity
    {
        public virtual void Build(ICompositeActivityBuilder composite)
        {
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var compositeActivityBlueprint = (ICompositeActivityBlueprint)context.ActivityBlueprint;
            var startActivities = compositeActivityBlueprint.GetStartActivities().Select(x => x.Id).ToList();
            return Combine(Done(), Schedule(startActivities, null!));
        }
    }
}