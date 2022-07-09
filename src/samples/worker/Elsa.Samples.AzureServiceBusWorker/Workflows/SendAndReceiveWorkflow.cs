using Elsa.Activities.AzureServiceBus;
using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.AzureServiceBusWorker.Workflows
{
    public class SendAndReceiveWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine(ctx =>
            {
                var correlationId = System.Guid.NewGuid().ToString("n");
                ctx.WorkflowInstance.CorrelationId = correlationId;

                return $"Start! - correlationId: {correlationId}";
            })
            .SendTopicMessage("testtopic2", "\"Hello World\"")
            .TopicMessageReceived("testtopic2", "testsub")
            .WriteLine(ctx => "End: " + (string)ctx.Input);
    }
}