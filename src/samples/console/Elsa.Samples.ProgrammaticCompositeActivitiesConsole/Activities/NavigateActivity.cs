using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;
using NetBox.Extensions;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Custom activities that inherit from <seealso cref="CompositeActivity"/> declare their own mini-workflow.
    /// </summary>
    [Action(Outcomes = new[] { "Left", "Right" })]
    public class NavigateActivity : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .StartWith(GetInstructions)
                .WriteLine(context => (string)context.GetInput<ActivityOutput>()!.Value)
                .ReadLine()
                .Finish(context => context.GetInput<string>().Capitalize());
        }

        private static void GetInstructions(ActivityExecutionContext context) => context.Output = new ActivityOutput("Turn left or right?");
    }
}