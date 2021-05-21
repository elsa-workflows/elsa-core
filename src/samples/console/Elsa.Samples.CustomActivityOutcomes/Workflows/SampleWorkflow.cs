using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Samples.CustomActivityOutcomes.Activities;

namespace Elsa.Samples.CustomActivityOutcomes.Workflows
{
    /// <summary>
    /// Demonstrates connecting outcomes of activities to other activities.
    /// </summary>
    public class SampleWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Trying something.").WithName("Start")
                .Then<TrySomething>(trySomething =>
                {
                    trySomething
                        .When(OutcomeNames.Done)
                        .WriteLine("Success!")
                        .ThenNamed("PromptTryAgain");
                    
                    trySomething
                        .When("Failed")
                        .WriteLine("That didn't work. Better luck next time.")
                        .ThenNamed("PromptTryAgain");
                })
                .Add<WriteLine>(x => x.WithText("Try again? (y/n)")).WithName("PromptTryAgain")
                .ReadLine()
                .If(context => string.Equals(context.GetInput<string>(), "y", StringComparison.OrdinalIgnoreCase), @if =>
                {
                    @if.When(OutcomeNames.True).ThenNamed("Start");
                    @if.When(OutcomeNames.False).WriteLine("See you around!");
                });
        }
    }
}