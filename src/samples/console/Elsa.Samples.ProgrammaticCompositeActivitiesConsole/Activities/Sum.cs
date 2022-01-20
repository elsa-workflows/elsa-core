using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Services;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Demonstrates accepting input properties and returning an output value from a composite activity.
    /// </summary>
    [Action(Outcomes = new[] { "Left", "Right" })]
    public class Sum : CompositeActivity
    {
        [ActivityInput]
        public int A
        {
            get => GetState<int>();
            set => SetState(value);
        }

        [ActivityInput]
        public int B
        {
            get => GetState<int>();
            set => SetState(value);
        }

        [ActivityOutput]
        public int Result
        {
            get => GetState<int>();
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .StartWith(c => Result = A + B);
        }
    }
}