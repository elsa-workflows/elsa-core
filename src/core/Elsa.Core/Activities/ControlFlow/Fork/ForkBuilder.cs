using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class ForkBuilder
    {
        private readonly IActivityBuilder _activityBuilder;

        public ForkBuilder(IActivityBuilder activityBuilder)
        {
            _activityBuilder = activityBuilder;
        }

        public IOutcomeBuilder When(string branch)
        {
            //_activityBuilder.
            //_fork.Branches.Add(branch);
            return _activityBuilder.When(branch);
        }
    }
}