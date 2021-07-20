using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.CustomOutcomeActivityConsole.Activities;

namespace Elsa.Samples.CustomOutcomeActivityConsole.Workflows
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class DemoWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine("Running demo workflow.")
            .Then<SomeCustomActivity>().When("Next")
            .WriteLine("Done!");
    }
}