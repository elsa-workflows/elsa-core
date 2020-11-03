using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Samples.GoBackConsole.Activities;

namespace Elsa.Samples.GoBackConsole.Workflows
{
    public class WalkAroundWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Taking a stroll...")
                .IfElse(context => (string?)context.Input == "Brick Wall",
                    ifElse =>
                    {
                        ifElse
                            .When(IfElse.False)
                            .WriteLine("Hitting a wall...")
                            .Then<BrickWallActivity>();

                        ifElse
                            .When(IfElse.True)
                            .WriteLine("Going around the wall...")
                            .WriteLine("Made it!");
                    });
        }
    }
}