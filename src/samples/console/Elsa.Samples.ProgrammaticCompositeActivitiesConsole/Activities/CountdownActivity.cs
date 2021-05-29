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
        // Think of composite activities as workflows and their properties as inputs into the workflow.
        [ActivityInput(Hint = "The start number.")]
        public int Start
        {
            get => GetState<int>();
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .For(() => Start, () => 0, () => -1, iterate =>
                {
                    iterate.WriteLine(context => $"{context.ForScope().CurrentValue}...");
                }, Operator.GreaterThan)
                .WriteLine("Happy New Year!");
        }
    }
}