using Elsa.Builders;
using Elsa.Models;
using Elsa.Testing.Shared.Unit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Elsa.Activities.Console;
using Elsa.Activities.AzureServiceBus;
using Elsa.Activities.AzureServiceBus.Services;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus;
using System.Threading;
using Elsa.Runtime;
using Elsa.Activities.AzureServiceBus.StartupTasks;
using Elsa.Services;
using Elsa.Services.Models;
using System.Reflection;
using Elsa.Services.Workflows;
using Elsa.Activities.AzureServiceBus.Bookmarks;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ServiceBusWorkflowTest : WorkflowsUnitTestBase
    {
        private static readonly Mock<ITopicMessageSenderFactory> TopicMessageSenderFactory = new();
        private static readonly Mock<ITopicMessageReceiverFactory> TopicMessageReceiverFactory = new();
        private static readonly Mock<ISenderClient> SenderClient = new();
        private static readonly Mock<IReceiverClient> ReceiverClient = new();
        private static readonly Mock<IWorkflowRegistry> WorkflowRegistryMoq = new();
        private static readonly AutoResetEvent WaitHandleTest = new(false);
        private static IWorkflowBlueprint _serviceBusBluePrint = default!;

        public ServiceBusWorkflowTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper,
                services =>
                {
                    services
                        .AddSingleton(TopicMessageSenderFactory.Object)
                        .AddSingleton(TopicMessageReceiverFactory.Object)
                        .AddSingleton<IWorkflowLaunchpad, WorkflowLaunchpad>()
                        .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                        .AddSingleton<IServiceBusTopicsStarter, ServiceBusTopicsStarter>()
                        .AddSingleton(WorkflowRegistryMoq.Object)
                        .AddBookmarkProvider<TopicMessageReceivedBookmarkProvider>()
                        .AddStartupTask<StartServiceBusTopics>();

                    services
                        .AddSingleton(new ServiceBusWorkflow(WaitHandleTest));
                },
                options =>
                {
                    options
                        .AddActivity<SendAzureServiceBusTopicMessage>()
                        .AddActivity<AzureServiceBusTopicMessageReceived>()
                        ;
                })
        {
            Func<Message, CancellationToken, Task> handler = default!;

            SenderClient
                .Setup(x => x.SendAsync(It.IsAny<Microsoft.Azure.ServiceBus.Message>()))
                .Callback<Message>(msg =>
                {
                    // Hack needed to avoid issue when getting msg from SB.
                    var systemProperties = msg.SystemProperties;

                    var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty;
                    var value = DateTime.UtcNow.AddMinutes(1);

                    systemProperties.GetType().InvokeMember("EnqueuedTimeUtc", bindings, Type.DefaultBinder, systemProperties, new object[] { value });
                    systemProperties.GetType().InvokeMember("SequenceNumber", bindings, Type.DefaultBinder, systemProperties, new object[] { 1 });
                    bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty;
                    msg.GetType().InvokeMember("SystemProperties", bindings, Type.DefaultBinder, msg, new object[] { systemProperties });
                    handler!.Invoke(msg, new CancellationToken());
                })
                .Returns(Task.FromResult(1));

            ReceiverClient
                .Setup(x => x.RegisterMessageHandler(It.IsAny<Func<Message, CancellationToken, Task>>(), It.IsAny<MessageHandlerOptions>()))
                .Callback<Func<Message, CancellationToken, Task>, MessageHandlerOptions>((messageHandler, _) => { handler = messageHandler; });

            ReceiverClient
                .Setup(x => x.Path)
                .Returns($"testtopic2/subscriptions/testsub");

            TopicMessageSenderFactory
                .Setup(x => x.GetTopicSenderAsync(It.IsAny<string>(), default))
                .Returns(Task.FromResult(SenderClient.Object));

            TopicMessageReceiverFactory
                .Setup(x => x.GetTopicReceiverAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(Task.FromResult(ReceiverClient.Object));

            _serviceBusBluePrint = WorkflowBuilder.Build<ServiceBusWorkflow>();

            WorkflowRegistryMoq
                .Setup(x => x.ListActiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<IWorkflowBlueprint>() { _serviceBusBluePrint }.AsEnumerable()));

            WorkflowRegistryMoq
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<VersionOptions>(), It.IsAny<CancellationToken>(), false))
                .Returns(Task.FromResult(_serviceBusBluePrint)!);
        }

        [Fact(DisplayName = "Send Message Bus after Suspend to avoid receiving response before indexing bookmark.")]
        public async Task SendRequestAfterSuspend()
        {
            await WorkflowStarter.StartWorkflowAsync(_serviceBusBluePrint);
            var result = WaitHandleTest.WaitOne(TimeSpan.FromSeconds(10));
            Assert.True(result);
        }
    }

    public class ServiceBusWorkflow : IWorkflow
    {
        private readonly AutoResetEvent _autoEvent;
        public ServiceBusWorkflow(AutoResetEvent autoEvent) => _autoEvent = autoEvent;

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine(ctx => $"Start! - correlationId: {ctx.WorkflowInstance.CorrelationId}")
                .SendTopicMessage(setup =>
                {
                    setup.Set(x => x.TopicName, "testtopic2");
                    setup.Set(x => x.Message, "\"Hello World\"");
                    setup.Set(x => x.SendMessageOnSuspend, true);
                })
                .TopicMessageReceived<string>("testtopic2", "testsub")
                .Then(() => _autoEvent.Set())
                .WriteLine(ctx => "End: " + (string)ctx.Input!);
        }
    }
}