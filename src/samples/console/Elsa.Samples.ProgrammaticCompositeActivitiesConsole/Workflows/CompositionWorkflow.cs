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
        public void Build(IWorkflowBuilder workflow) => workflow
            .WriteLine("Welcome to the Composite Activities demo workflow!")
            
            // A custom, composite activity
            .Then<CountDownActivity>()
            .WriteLine("Done")
        ;
    }
}