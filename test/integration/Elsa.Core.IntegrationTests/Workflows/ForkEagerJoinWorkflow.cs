using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkEagerJoinWorkflow : IWorkflow
    {
        private readonly bool _eagerJoin;
        
        public ForkEagerJoinWorkflow(bool eagerJoin)
        {
            _eagerJoin = eagerJoin;
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<Fork>(
                    activity => activity.Set(x => x.Branches, new HashSet<string>(new[] { "Branch 1", "Branch 2" })),
                    fork =>
                    {
                        fork.When("Branch 1").SignalReceived("Signal1");
                        fork.When("Branch 2").ThenNamed("Join");
                    })
                .Add<Join>(join => join.Set(x => x.Mode, Join.JoinMode.WaitAny).Set(x => x.EagerJoin, _eagerJoin)).WithName("Join")
                .SignalReceived("Signal3")
                .WriteLine("Finished");
        }
    }
}

