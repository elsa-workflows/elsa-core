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
            .Then<CountdownActivity>(activity => activity.Set(x => x.Start, 10))
            .WriteLine("=Sum demo=").WithName("SumDemo")
            .WriteLine("Enter first value:")
            .ReadLine().WithName("ValueA")
            .WriteLine("Enter second value:")
            .ReadLine().WithName("ValueB")
            .Then<Sum>(activity => activity
                .Set(x => x.A, async context =>
                {
                    var a = (await context.WorkflowExecutionContext.GetNamedActivityPropertyAsync<ReadLine, string>("ValueA", r => r.Output))!;
                    return int.Parse(a);
                })
                .Set(x => x.B, async context =>
                {
                    var b = (await context.WorkflowExecutionContext.GetNamedActivityPropertyAsync<ReadLine, string>("ValueB", r => r.Output))!;
                    return int.Parse(b);
                })
            ).WithName("Sum1")
            .WriteLine(async context => $"Sum: {await context.GetNamedActivityPropertyAsync<Sum, int>("Sum1", x => x.Result)}");
    }
}