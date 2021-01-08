using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Services;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Custom activities that inherit from <seealso cref="CompositeActivity"/> declare their own mini-workflow.
    /// </summary>
    public class CountdownActivity : CompositeActivity
    {
        // Exposed properties on composite activities must store their values in the State property bag so that it gets persisted & available to child activities.
        // Activities are scheduled one by one, which means that when the child activities are executed, the container activity (such as this one) will not be in memory - the default value will be captured when the property is used directly.
        [ActivityProperty(Hint = "The start number.")]
        public int Start
        {
            get => GetState<int>();
            set => SetState(value);
        }
        
        public override void Build(ICompositeActivityBuilder activity)
        {
            activity
                .For(context => context.GetParentState<int>(nameof(Start)), _ => 0, _ => -1, iterate =>
                {
                    iterate.WriteLine(context => $"{context.GetInput<int>()}...");
                }, Operator.GreaterThan)
                .WriteLine("Happy New Year!");
        }
    }
}