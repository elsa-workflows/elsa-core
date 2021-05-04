using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.AutoConnectNextActivityConsole.Activities;

namespace Elsa.Samples.AutoConnectNextActivityConsole.Workflows
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class Demoworkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine("Running demo workflow.")
            .Then<SomeCustomActivity>() // Even though this activity returns "Next", we still want to execute the next activity (which is connected via "Done").
            .WriteLine("Done!");
    }
}