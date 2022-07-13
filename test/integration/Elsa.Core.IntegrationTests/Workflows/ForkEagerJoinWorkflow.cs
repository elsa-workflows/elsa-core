using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkEagerJoinWorkflow : IWorkflow
    {
        private readonly bool _eagerJoin;
        private readonly string[] _branches;

        public ForkEagerJoinWorkflow(bool eagerJoin, bool reverseBranchOrder)
        {
            _eagerJoin = eagerJoin;

            _branches = new[] { "Branch 1", "Branch 2" };

            if (reverseBranchOrder) _branches = _branches.Reverse().ToArray();
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<Fork>(
                    activity => activity.Set(x => x.Branches, new HashSet<string>(_branches)),
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

