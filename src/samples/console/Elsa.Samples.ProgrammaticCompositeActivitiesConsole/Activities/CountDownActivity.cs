using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Custom activities that inherit from <seealso cref="CompositeActivity"/> declare their own mini-workflow.
    /// </summary>
    [Action(Outcomes = new[] { "Left", "Right" })]
    public class CountDownActivity : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder activity)
        {
            activity
                .StartWith(GetInstructions)
                .WriteLine(context => (string)context.Input)
                .ReadLine()
                .IfElse(context => string.Equals(context.GetInput<string>(), "left", StringComparison.CurrentCultureIgnoreCase), ifElse =>
                {
                    ifElse.When(IfElse.True).WriteLine("We're going left");
                    ifElse.When(IfElse.False).WriteLine("We're going right");
                });
        }

        private static void GetInstructions(ActivityExecutionContext context) => context.Output = "Turn left or right?";
    }
}