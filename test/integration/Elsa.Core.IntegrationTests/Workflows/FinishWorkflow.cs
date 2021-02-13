using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class FinishWorkflow : IWorkflow
    {
        private readonly object _output;
        public FinishWorkflow(object output) => _output = output;
        public void Build(IWorkflowBuilder builder) => builder.Finish(finish => finish.WithOutput(_output));
    }
}