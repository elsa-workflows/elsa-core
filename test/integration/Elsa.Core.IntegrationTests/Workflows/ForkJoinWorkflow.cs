using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkJoinWorkflow : IWorkflow
    {
        private readonly Join.JoinMode _joinMode;

        public ForkJoinWorkflow(Join.JoinMode joinMode)
        {
            _joinMode = joinMode;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow.StartWith<Fork>(
                    activity => activity.Set(x => x.Branches, new HashSet<string>(new[] { "Branch 1", "Branch 2", "Branch 3" })),
                    fork =>
                    {
                        fork.When("Branch 1").Signaled("Signal1").WriteLine("Branch 1 executed", "WriteLine1").Then("Join");
                        fork.When("Branch 2").Signaled("Signal2").WriteLine("Branch 2 executed", "WriteLine2").Then("Join");
                        fork.When("Branch 3").Signaled("Signal3").WriteLine("Branch 3 executed", "WriteLine3").Then("Join");
                    })
                .Add<Join>(join => join.Set(x => x.Mode, _joinMode)).WithName("Join")
                .WriteLine("Finished", "Finished");
        }
    }
}