using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkJoinWaitAllWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow.StartWith<Fork>(
                    activity => activity.Set(x => x.Branches, new HashSet<string>(new[] { "Branch 1", "Branch 2", "Branch 3" })),
                    fork =>
                    {
                        fork.When("Branch 1").WriteLine("Branch 1 executed").Then("Join");
                        fork.When("Branch 2").WriteLine("Branch 2 executed").Then("Join");
                        fork.When("Branch 3").WriteLine("Branch 3 executed").Then("Join");
                    })
                .Add<Join>(join => join.Set(x => x.Mode, Join.JoinMode.WaitAll)).WithName("Join")
                .WriteLine("Finished");
        }
    }
}