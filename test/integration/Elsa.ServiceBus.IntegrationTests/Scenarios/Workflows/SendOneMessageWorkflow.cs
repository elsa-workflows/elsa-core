﻿using Elsa.AzureServiceBus.Activities;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Scenarios.Workflows;

public class SendOneMessageWorkflow(ITestResetEventManager waitHandleTestManager) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new SendMessage
                {
                    QueueOrTopic = new("sendTopic1"),
                    MessageBody = new ("Hello World"),
                },
                new MessageReceived("topicName", "subscriptionName"),
                new WriteLine(_ =>
                {
                    waitHandleTestManager.Set("receive1");
                    return "first receive ok";
                }),
            }
        };
    }
}

public class SendOneMessageWithCorrelationIdWorkflow(ITestResetEventManager waitHandleTestManager) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new SendMessage
                {
                    QueueOrTopic = new("sendTopic1"),
                    MessageBody = new("Hello World"),
                },
                new Correlate
                {
                    CorrelationId = new("EEE3D9CC-2279-4CE5-8F4F-FC6C65BF8814")
                },
                new MessageReceived("topicName", "subscriptionName"),
                new WriteLine(context =>
                {
                    waitHandleTestManager.Set("receive1");
                    return "first receive ok";
                }),
            }
        };
    }
}