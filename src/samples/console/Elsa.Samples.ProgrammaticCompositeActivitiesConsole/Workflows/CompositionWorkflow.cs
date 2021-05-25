using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Workflows
{
    /// <summary>
    /// A basic workflow demonstrating the use of composite activities.
    /// </summary>
    public class CompositionWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine("Welcome to the Composite Activities demo workflow!")

            // A custom, composite activity
            .WriteLine("=Navigation demo=")
            .Then<NavigateActivity>(countDown =>
            {
                countDown.When("Left").WriteLine("Where going left!").ThenNamed("CountdownDemo");
                countDown.When("Right").WriteLine("Where going right!").ThenNamed("CountdownDemo");
            })
            .WriteLine("=Countdown demo=").WithName("CountdownDemo")
            .Then<CountdownActivity>(activity => activity.Set(x => x.Start, 10));
    }
}