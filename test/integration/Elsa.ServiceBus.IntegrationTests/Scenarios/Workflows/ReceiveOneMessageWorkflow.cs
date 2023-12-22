using Elsa.AzureServiceBus.Activities;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Scenarios.Workflows;

public class ReceiveOneMessageWorkflow(ITestResetEventManager waitHandleTestManager) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence()
        {
            Activities =
            {
                new MessageReceived("topicName","subscriptionName"),
                new WriteLine(context=> {waitHandleTestManager.Set("receive1"); return "first receive ok"; }),
            }
        };
    }
}