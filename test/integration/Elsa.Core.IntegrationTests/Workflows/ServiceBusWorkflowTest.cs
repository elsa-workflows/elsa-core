using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using Elsa.Activities.AzureServiceBus;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Activities.AzureServiceBus.StartupTasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.Workflows;
using Elsa.Testing.Shared.Unit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ServiceBusWorkflowTest : WorkflowsUnitTestBase
    {
        private const string ConnectionString = ""; // Put your ASB connection string here if you want to test.
        private static readonly Mock<IWorkflowRegistry> WorkflowRegistryMoq = new();
        private static readonly AutoResetEvent WaitHandleTest = new(false);
        private static IWorkflowBlueprint _serviceBusBlueprint = default!;

        public ServiceBusWorkflowTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper,
                services => {},
                containerBuilder =>
                {
                    containerBuilder
                        .AddMultiton<IWorkflowLaunchpad, WorkflowLaunchpad>()
                        .AddMultiton<IWorkerManager, WorkerManager>()
                        .AddMultiton(WorkflowRegistryMoq.Object);

                    containerBuilder
                        .AddMultiton(new ServiceBusWorkflow(WaitHandleTest));
                },
                options =>
                {
                    options.ContainerBuilder
                        .AddBookmarkProvider<MessageReceivedBookmarkProvider>()
                        .AddHostedService<StartWorkers>();

                    options
                        .AddAzureServiceBusActivities(option => option.ConnectionString = ConnectionString)
                        ;
                })
        {
            
            _serviceBusBlueprint = WorkflowBuilder.Build<ServiceBusWorkflow>();

            WorkflowRegistryMoq
                .Setup(x => x.FindAsync(It.IsAny<string>(), It.IsAny<VersionOptions>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_serviceBusBlueprint);
        }

        [Fact(DisplayName = "Send Message Bus after Suspend to avoid receiving response before indexing bookmark.")]
        public async Task SendRequestAfterSuspend()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                TestOutputHelper.WriteLine("Azure eService Bus integration test is disabled. Provide a connection string to enable.");
                return;
            }
            
            await WorkflowStarter.StartWorkflowAsync(_serviceBusBlueprint);
            var result = WaitHandleTest.WaitOne(TimeSpan.FromSeconds(1000));
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
                .TopicMessageReceived("testtopic2", "testsub")
                .Then(() => _autoEvent.Set())
                .WriteLine(ctx => "End: " + (string)ctx.Input!);
        }
    }
}