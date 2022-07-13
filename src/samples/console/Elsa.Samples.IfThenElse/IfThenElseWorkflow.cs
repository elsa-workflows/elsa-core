using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.IfThenElse
{
    /// <summary>
    /// This workflow prompts the user to enter their age. Depending on the value, the user is granted access to an alcoholic beverage.
    /// </summary>
    public class IfThenElseWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Please enter your age:")
                .ReadLine()
                .SetVariable("Age", context => context.GetInput<int>())
                .If(context => context.GetVariable<int>("Age") > 21, @if =>
                {
                    @if.When(OutcomeNames.True).WriteLine("Enjoy your alcoholic beverage!");
                    @if.When(OutcomeNames.False).WriteLine("Enjoy your coca cola!");
                })
                .WriteLine("Cheers!");
        }
    }
}