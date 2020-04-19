using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class SwitchBuilder
    {
        private readonly IActivityBuilder activityBuilder;
        private readonly Switch @switch;
        
        public SwitchBuilder(IActivityBuilder activityBuilder)
        {
            this.activityBuilder = activityBuilder;
            @switch = (Switch)activityBuilder.Activity;
        }

        public IOutcomeBuilder When(string @case)
        {
            @switch.Cases.Add(@case);
            return activityBuilder.When(@case);
        }
    }
}