using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Samples.GoBackConsole.Activities;

namespace Elsa.Samples.GoBackConsole.Workflows
{
    public class WalkAroundWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Taking a stroll...")
                .If(context => (string?)context.Input == "Brick Wall",
                    @if =>
                    {
                        @if
                            .When(If.False)
                            .WriteLine("Hitting a wall...")
                            .Then<BrickWallActivity>();

                        @if
                            .When(If.True)
                            .WriteLine("Going around the wall...")
                            .WriteLine("Made it!");
                    });
        }
    }
}