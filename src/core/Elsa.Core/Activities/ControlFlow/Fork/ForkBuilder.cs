using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class ForkBuilder
    {
        private readonly IActivityBuilder activityBuilder;
        private readonly Fork fork;
        
        public ForkBuilder(IActivityBuilder activityBuilder)
        {
            this.activityBuilder = activityBuilder;
            fork = (Fork)activityBuilder.Activity;
        }

        public IOutcomeBuilder When(string branch)
        {
            fork.Branches.Add(branch);
            return activityBuilder.When(branch);
        }
    }
}