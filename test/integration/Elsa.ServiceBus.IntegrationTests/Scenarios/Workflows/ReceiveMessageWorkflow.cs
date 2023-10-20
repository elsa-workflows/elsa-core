using Elsa.AzureServiceBus.Activities;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Scenarios.Workflows
{
    public class ReceiveMessageWorkflow : WorkflowBase
    {
        private readonly ITestResetEventManager _waitHandleTestManager;

        public ReceiveMessageWorkflow(ITestResetEventManager waitHandleTestManager)
        {
            _waitHandleTestManager = waitHandleTestManager;
        }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence()
            {
                Activities =
                {
                    new MessageReceived("topicName","subscriptionName"),
                    new WriteLine(context=> {_waitHandleTestManager.Set("receive1"); return "first receive ok"; }),
                    new MessageReceived("topicName1","subscriptionName1"),
                    new WriteLine(context=> {_waitHandleTestManager.Set("receive2"); return "Ok"; }),
                }
            };
        }

    }
}