using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.Forks.Workflows
{
    /// <summary>
    /// Demonstrates connecting outcomes of activities to other activities.
    /// </summary>
    public class SimpleForkWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var users = new[] { "john", "tom", "jenny" };

            builder
                .WriteLine("This demonstrates a simple workflow with switch.")
                .WriteLine("Using switch we can branch a workflow.")
                .Then<Fork>(fork => fork.WithBranches(users), fork =>
                {
                    foreach (var user in users)
                        fork.When(user)
                            .WriteLine("A fork created for " + user)
                            .ThenNamed("Join1");
                })
                .Then<Join>(join => join.WithMode(Join.JoinMode.WaitAll)).WithName("Join1")
                .WriteLine("Workflow finished.");
        }
    }
}