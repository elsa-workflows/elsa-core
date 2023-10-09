using Azure.Messaging.ServiceBus;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.ObjectModel;
using Xunit.Abstractions;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Microsoft.Extensions.Options;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.ServiceBusIntegrationTests.Contracts;
using Elsa.ServiceBusIntegrationTests.Scenarios.workflows;
using Elsa.ServiceBusIntegrationTests.Helpers;

namespace Elsa.ServiceBusIntegrationTests.Scenarios.ServiceBus
{

    public class ServiceBusTest : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly CapturingTextWriter _capturingTextWriter = new();

        private readonly Mock<ServiceBusClient> _serviceBusClient = new();

        private IWorkerManager _worker;
        private readonly BackgroundCommandSenderHostedService _hosted;
        private readonly BackgroundEventPublisherHostedService _backgroundEventService;
        private readonly ITestResetEventManager _resetEventManager = new TestResetEventManager();
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IServiceBusProcessorManager _sbProcessorManager;

        public ServiceBusTest(ITestOutputHelper testOutputHelper)
        {
            _services = new TestApplicationBuilder(testOutputHelper)
                .WithCapturingTextWriter(_capturingTextWriter)
                .AddWorkflow<ReceiveOneMessageWorkflow>()
                .AddWorkflow<ReceiveMessageWorkflow>()
                .AddWorkflow<SendOneMessageWorkflow>()
                .AddWorkflow<SendOneMessageWithCorrelationIdWorkflow>()
                .ConfigureServices(services =>
                {
                    services
                    .AddSingleton<IMock<ServiceBusClient>>(_serviceBusClient)
                    .AddSingleton(_serviceBusClient.Object)
                    .AddSingleton<IServiceBusProcessorManager, ServiceBusProcessorManager>()

                    .AddSingleton<IWorkerManager, WorkerManager>()
                    .AddSingleton(_resetEventManager)

                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundCommandSenderHostedService>(sp, options.CommandWorkerCount);
                    })
                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundEventPublisherHostedService>(sp, options.NotificationWorkerCount);
                    })
                    ;
                })
                .Build();

            _worker = _services.GetRequiredService<IWorkerManager>();
            _hosted = _services.GetRequiredService<BackgroundCommandSenderHostedService>();
            _backgroundEventService = _services.GetRequiredService<BackgroundEventPublisherHostedService>();
            _sbProcessorManager = _services.GetRequiredService<IServiceBusProcessorManager>();

            _testOutputHelper = testOutputHelper;
        }

        [Fact(DisplayName = "2 Receive - Sending 1 message - Should Block")]
        public async Task Receive_1_Message_Should_Block_If_One_Receive()
        {
            _sbProcessorManager.Init("topicName", "subscriptionName");
            _sbProcessorManager.Init("topicName1", "subscription1");

            //Init waitEvent :
            _resetEventManager.Init("receive1");
            _resetEventManager.Init("receive2");

            //Init BackGround
            await InitRegistryAndBackGroundServiceWorkerAsync();

            //Start Workflow
            var workflowDefinitionId = typeof(ReceiveMessageWorkflow).Name;
            var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), Common.Models.VersionOptions.Published);
            var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
            var workflowState = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);

            /*
             * Workflow don't receive any message so it should be
             * Running
             * Suspended
             */
            Assert.Equal(WorkflowStatus.Running, workflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

            //Start Worker to send Message on topicName/suscriptionName
            await _worker.StartWorkerAsync("topicName", "subscriptionName");
            await _sbProcessorManager
                .Get("topicName", "subscriptionName")
                .SendMessage<dynamic>(new { hello = "world" }, null);

            //Wait for receiving first message
            var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait1 : {wait1}");

            //Wait for receiving second message
            var wait2 = _resetEventManager.Get("receive2").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait2 : {wait2}");

            await Task.Delay(500); //Todo find how to remove delay
            var lastWorkflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);
            /*
             * We don't send 2 messages so Workflow must be 
             * Running
             * Suspended
             * 
             * with a timeout on Wait for 2nd Message. (False ResetEvent)
             */
            Assert.NotNull(lastWorkflowState);
            Assert.Equal(WorkflowStatus.Running, lastWorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, lastWorkflowState.SubStatus);
            Assert.True(wait1);
            Assert.False(wait2);
        }

        private async Task InitRegistryAndBackGroundServiceWorkerAsync()
        {
            //Init Registries to use StartWorkflow
            await _services.PopulateRegistriesAsync();

            //Start background services for CommandHandler
            await _hosted.StartAsync(CancellationToken.None);
            await _backgroundEventService.StartAsync(CancellationToken.None);
        }

        [Fact(DisplayName = "1 Receive - Sending 1 message - Should Finished")]
        public async Task Receive_1_Message_Should_Finish_With_One_Receive()
        {
            _sbProcessorManager.Init("topicName", "subscriptionName");

            //Init waitEvent :
            _resetEventManager.Init("receive1");

            //Init BackGround
            await InitRegistryAndBackGroundServiceWorkerAsync();

            //Start Workflow
            var workflowDefinitionId = typeof(ReceiveOneMessageWorkflow).Name;
            var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), Common.Models.VersionOptions.Published);
            var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
            var workflowState = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);

            /*
             * Workflow don't receive any message so it should be
             * Running
             * Suspended
             */
            Assert.Equal(WorkflowStatus.Running, workflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

            //Start Worker to send Message on topicName/suscriptionName
            await _worker.StartWorkerAsync("topicName", "subscriptionName");
            await _sbProcessorManager
                .Get("topicName", "subscriptionName")
                .SendMessage<dynamic>(new { hello = "world" }, null);

            //Wait for receiving first message
            var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait1 : {wait1}");

            await Task.Delay(500); //Todo find how to remove delay
            var lastWorkflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);
            /*
             * We sent 1 message so Workflow must be 
             * Finished
             * Finished
             * 
             * with no timeout for the first message. (True ResetEvent)
             */
            Assert.NotNull(lastWorkflowState);
            Assert.Equal(WorkflowStatus.Finished, lastWorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Finished, lastWorkflowState.SubStatus);
            Assert.True(wait1);
        }

        [Fact(DisplayName = "1 Send - 1 Receive - Should Finished - without race condition")]
        public async Task Send_1_Message_And_Receive_Response()
        {
            _sbProcessorManager.Init("topicName", "subscriptionName");

            //Init waitEvent :
            _resetEventManager.Init("receive1");

            //Init BackGround
            await InitRegistryAndBackGroundServiceWorkerAsync();

            var senderMock = new Mock<ServiceBusSender>();
            _serviceBusClient
                .Setup(sb =>
                sb.CreateSender(It.IsAny<string>())
                )
                .Returns(senderMock.Object);
            senderMock
                .Setup(sender =>
                    sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>(async (sb, c) =>
                {
                    _testOutputHelper.WriteLine("Sending Message from activity");

                    await _worker.StartWorkerAsync("topicName", "subscriptionName");
                    await _sbProcessorManager
                        .Get("topicName", "subscriptionName")
                        .SendMessage<dynamic>(new { hello = "world" }, null);
                })
                .Returns(Task.CompletedTask);

            //Start Workflow
            var workflowDefinitionId = typeof(SendOneMessageWorkflow).Name;
            var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), Common.Models.VersionOptions.Published);
            var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
            var workflowState = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);

            /*
             * Workflow don't receive any message so it should be
             * Running
             * Suspended
             */
            Assert.Equal(WorkflowStatus.Running, workflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);


            //Start Worker to send Message on topicName/suscriptionName
            //await _worker.StartWorkerAsync("topicName", "subscriptionName");
            //await _sbProcessorManager
            //    .Get("topicName", "subscriptionName")
            //    .SendMessage<dynamic>(new { hello = "world" }, null);

            //Wait for receiving first message
            var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait1 : {wait1}");

            await Task.Delay(500); //Todo find how to remove delay
            var lastWorkflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);
            /*
             * We sent 1 message so Workflow must be 
             * Finished
             * Finished
             * 
             * with no timeout for the first message. (True ResetEvent)
             */
            Assert.NotNull(lastWorkflowState);
            Assert.Equal(WorkflowStatus.Finished, lastWorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Finished, lastWorkflowState.SubStatus);
            Assert.True(wait1);
        }

        [Fact(DisplayName = "1 Send - 1 Receive - with correlationId - Should Finished - without race condition")]
        public async Task Send_1_Message_And_Correlate_Receive_Response()
        {
            var correlationId = "EEE3D9CC-2279-4CE5-8F4F-FC6C65BF8814";
            _sbProcessorManager.Init("topicName", "subscriptionName");
            
            //Init waitEvent :
            _resetEventManager.Init("receive1");

            //Init BackGround
            await InitRegistryAndBackGroundServiceWorkerAsync();

            var senderMock = new Mock<ServiceBusSender>();
            _serviceBusClient
                .Setup(sb =>
                sb.CreateSender(It.IsAny<string>())
                )
                .Returns(senderMock.Object);
            senderMock
                .Setup(sender =>
                    sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>(async (sb, c) =>
                {
                    _testOutputHelper.WriteLine("Sending Message from activity");

                    await _worker.StartWorkerAsync("topicName", "subscriptionName");
                    await _sbProcessorManager
                        .Get("topicName", "subscriptionName")
                        .SendMessage<dynamic>(new { hello = "world" }, correlationId);
                })
                .Returns(Task.CompletedTask);

            //Start Workflow
            var workflowDefinitionId = typeof(SendOneMessageWithCorrelationIdWorkflow).Name;
            var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), Common.Models.VersionOptions.Published);
            var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
            var workflowState = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);

            /*
             * Workflow don't receive any message so it should be
             * Running
             * Suspended
             */
            Assert.Equal(WorkflowStatus.Running, workflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);


            //Start Worker to send Message on topicName/suscriptionName
            //await _worker.StartWorkerAsync("topicName", "subscriptionName");
            //await _sbProcessorManager
            //    .Get("topicName", "subscriptionName")
            //    .SendMessage<dynamic>(new { hello = "world" }, null);

            //Wait for receiving first message
            var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait1 : {wait1}");

            await Task.Delay(500); //Todo find how to remove delay
            var lastWorkflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);
            /*
             * We sent 1 message so Workflow must be 
             * Finished
             * Finished
             * 
             * with no timeout for the first message. (True ResetEvent)
             */
            Assert.NotNull(lastWorkflowState);
            Assert.Equal(WorkflowStatus.Finished, lastWorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Finished, lastWorkflowState.SubStatus);
            Assert.True(wait1);
        }


        [Fact(DisplayName = "1 Receive - Listening from other topic - Should not trigger")]
        public async Task Receive_1_Message_Should_Not_Trigger()
        {
            _sbProcessorManager.Init("topicName1", "subscriptionName1");
            
            //Init waitEvent :
            _resetEventManager.Init("receive1");
            _resetEventManager.Init("receive2");

            //Init BackGround
            await InitRegistryAndBackGroundServiceWorkerAsync();

            //Start Workflow
            var workflowDefinitionId = typeof(ReceiveMessageWorkflow).Name;
            var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), Common.Models.VersionOptions.Published);
            var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
            var workflowState = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);

            /*
             * Workflow don't receive any message so it should be
             * Running
             * Suspended
             */
            Assert.Equal(WorkflowStatus.Running, workflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

            //Start Worker to send Message on topicName/suscriptionName
            await _worker.StartWorkerAsync("topicName1", "subscriptionName1");
            await _sbProcessorManager
                .Get("topicName1", "subscriptionName1")
                .SendMessage<dynamic>(new { hello = "world" }, null);

            //Wait for receiving first message
            var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
            _testOutputHelper.WriteLine($"wait1 : {wait1}");

            var lastWorkflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);
            /*
             * We don't send 2 messages so Workflow must be 
             * Running
             * Suspended
             * 
             * with a timeout on Wait for 2nd Message. (False ResetEvent)
             */
            Assert.NotNull(lastWorkflowState);
            Assert.Equal(WorkflowStatus.Running, lastWorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Suspended, lastWorkflowState.SubStatus);
            Assert.False(wait1);
            
        }


        public void Dispose()
        {

            _hosted.StopAsync(new CancellationToken()).GetAwaiter().GetResult();
        }
    }
}