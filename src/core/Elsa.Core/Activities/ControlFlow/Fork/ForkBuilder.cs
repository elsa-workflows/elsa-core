using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class ForkBuilder
    {
        private readonly IActivityBuilder _activityBuilder;
        private readonly Fork _fork;
        
        public ForkBuilder(IActivityBuilder activityBuilder)
        {
            _activityBuilder = activityBuilder;
            //_fork = (Fork)activityBuilder.Activity;
        }

        public IOutcomeBuilder When(string branch)
        {
            _fork.Branches.Add(branch);
            return _activityBuilder.When(branch);
        }
    }
}