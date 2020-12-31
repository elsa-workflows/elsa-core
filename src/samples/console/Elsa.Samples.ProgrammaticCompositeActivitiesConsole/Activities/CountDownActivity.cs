using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.ActivityResults;
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
                .Finish(context => (string) context.Input);
        }

        protected override IActivityExecutionResult Complete(ActivityExecutionContext context) => Outcome(((string) context.WorkflowExecutionContext.WorkflowInstance.Output)!);
        
        private static void GetInstructions(ActivityExecutionContext context) => context.Output = "Turn left or right?";
    }
}