using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForLoopWorkflow : IWorkflow
    {
        private readonly int _loopCount;

        public ForLoopWorkflow(int loopCount)
        {
            _loopCount = loopCount;
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder.For(
                0,
                _loopCount,
                iterate => iterate
                    .Then<WriteLine>(activity => activity.Set(x => x.Text, context => $"Iteration {context.Input}"))
                    .WithId("WriteLine"));
        }
    }
}