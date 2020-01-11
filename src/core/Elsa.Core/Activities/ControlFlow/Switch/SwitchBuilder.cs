using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public class SwitchBuilder
    {
        private readonly ActivityBuilder activityBuilder;
        private readonly Switch @switch;
        
        public SwitchBuilder(ActivityBuilder activityBuilder)
        {
            this.activityBuilder = activityBuilder;
            @switch = (Switch)activityBuilder.Activity;
        }

        public OutcomeBuilder When(string @case)
        {
            @switch.Cases.Add(@case);
            return activityBuilder.When(@case);
        }
    }
}