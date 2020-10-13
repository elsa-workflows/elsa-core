using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class SwitchBuilder
    {
        private readonly IActivityBuilder _activityBuilder;
        private readonly Switch _switch;
        
        public SwitchBuilder(IActivityBuilder activityBuilder)
        {
            _activityBuilder = activityBuilder;
            //_switch = (Switch)activityBuilder.Activity;
        }

        public IOutcomeBuilder When(string @case)
        {
            _switch.Cases.Add(@case);
            return _activityBuilder.When(@case);
        }
    }
}