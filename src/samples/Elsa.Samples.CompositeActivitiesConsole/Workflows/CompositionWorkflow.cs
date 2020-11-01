using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.CompositeActivitiesConsole.Activities;

namespace Elsa.Samples.CompositeActivitiesConsole.Workflows
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class CompositionWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow) => workflow
            .WriteLine("Welcome to the Composite Activities demo workflow!")
            .Then<CountDownActivity>()
            .WriteLine("Done!");
    }
}