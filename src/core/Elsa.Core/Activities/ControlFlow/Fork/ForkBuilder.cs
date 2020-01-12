using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class ForkBuilder
    {
        private readonly ActivityBuilder activityBuilder;
        private readonly Fork fork;
        
        public ForkBuilder(ActivityBuilder activityBuilder)
        {
            this.activityBuilder = activityBuilder;
            fork = (Fork)activityBuilder.Activity;
        }

        public OutcomeBuilder When(string branch)
        {
            fork.Branches.Add(branch);
            return activityBuilder.When(branch);
        }
    }
}