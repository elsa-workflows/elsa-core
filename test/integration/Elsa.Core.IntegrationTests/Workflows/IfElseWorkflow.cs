using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class IfElseWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Start")
                .Then<IfElse>(
                    ifElse => ifElse.WithCondition(context => context.GetVariable<bool>("Flag")),
                    ifElse =>
                    {
                        ifElse
                            .When(IfElse.False)
                            .WriteLine("Flag is false. Setting it to true.")
                            .SetVariable("Flag", true)
                            .Then("IfElse");

                        ifElse
                            .When(IfElse.True)
                            .WriteLine("Flag is set to true.")
                            .Then("Done");
                    })
                .WithId("IfElse")
                .WithName("IfElse")
                .Add<WriteLine>(writeLine => writeLine.WithText("Done")).WithName("Done");
        }
    }
}