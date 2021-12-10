using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class TimerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Starting timer workflow test")
                .Timer(Duration.FromSeconds(1))
                .WriteLine("Finishing timer workflow test");
        }
    }
}