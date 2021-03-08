using Elsa.Activities.UserTask.Activities;
using Elsa.Builders;
using Elsa.Activities.ControlFlow;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class SampleWorkflow : IWorkflow
    {
        public static readonly object Result = new object();

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<UserTask>(setup: s => {
                    s.Set(x => x.Actions, c => new [] { "Foo", "Bar" });
                })
                .Finish(x => x.WithOutput(Result));
        }
    }
}