using Azure.Messaging.ServiceBus;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Services;
using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using Elsa.ServiceBus.IntegrationTests.Helpers;
using Elsa.ServiceBus.IntegrationTests.Scenarios.Workflows;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Parameters;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ServiceBus.IntegrationTests.Scenarios.ServiceBus;

public class ServiceBusTest : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly ServiceBusClient _serviceBusClient = Substitute.For<ServiceBusClient>();
    private readonly IWorkerManager _worker;
    private readonly BackgroundCommandSenderHostedService _backgroundCommandSenderHostedService;
    private readonly BackgroundEventPublisherHostedService _backgroundEventPublisherHostedService;
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
                    .AddSingleton(_serviceBusClient)
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
        _backgroundCommandSenderHostedService = _services.GetRequiredService<BackgroundCommandSenderHostedService>();
        _backgroundEventPublisherHostedService = _services.GetRequiredService<BackgroundEventPublisherHostedService>();
        _sbProcessorManager = _services.GetRequiredService<IServiceBusProcessorManager>();

        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "2 Receive - Sending 1 message - Should Block")]
    public async Task Receive_1_Message_Should_Block_If_One_Receive()
    {
        _sbProcessorManager.Init("topicName", "subscriptionName");
        _sbProcessorManager.Init("topicName1", "subscription1");

        // Init waitEvent:
        _resetEventManager.Init("receive1");
        _resetEventManager.Init("receive2");

        // Init BackGround
        await InitRegistryAndBackGroundServiceWorkerAsync();

        // Start Workflow
        const string workflowDefinitionId = nameof(ReceiveMessageWorkflow);
        var workflowClient = await CreateWorkflowClientAsync(workflowDefinitionId);
        var response = await workflowClient.RunAsync(RunWorkflowInstanceRequest.Empty);

        /*
         * The workflow doesn't receive any messages, so it should be
         * Running
         * Suspended
         */
        Assert.Equal(WorkflowStatus.Running, response.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, response.SubStatus);

        // Start Worker to send Message on topicName/subscriptionName
        await _worker.StartWorkerAsync("topicName", "subscriptionName");
        await _sbProcessorManager
            .Get("topicName", "subscriptionName")
            .SendMessage<dynamic>(new { hello = "world" }, null!);

        // Wait for receiving the first message.
        var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait1 : {wait1}");

        // Wait for receiving second message
        var wait2 = _resetEventManager.Get("receive2").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait2 : {wait2}");

        await Task.Delay(500); // Todo find how to remove delay
        var lastWorkflowState = await workflowClient.ExportStateAsync();
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

    [Fact(DisplayName = "1 Receive - Sending 1 message - Should Finished")]
    public async Task Receive_1_Message_Should_Finish_With_One_Receive()
    {
        _sbProcessorManager.Init("topicName", "subscriptionName");

        // Init waitEvent:
        _resetEventManager.Init("receive1");

        // Init BackGround
        await InitRegistryAndBackGroundServiceWorkerAsync();

        // Start Workflow
        const string workflowDefinitionId = nameof(ReceiveOneMessageWorkflow);
        var workflowClient = await CreateWorkflowClientAsync(workflowDefinitionId);
        var workflowState = await workflowClient.RunAsync(RunWorkflowInstanceRequest.Empty);

        /*
         * Workflow don't receive any message so it should be
         * Running
         * Suspended
         */
        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        //Start Worker to send Message on topicName/subscriptionName
        await _worker.StartWorkerAsync("topicName", "subscriptionName");
        await _sbProcessorManager
            .Get("topicName", "subscriptionName")
            .SendMessage<dynamic>(new { hello = "world" }, null!);

        //Wait for receiving first message
        var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait1 : {wait1}");

        await Task.Delay(500); //Todo find how to remove delay
        var lastWorkflowState = await workflowClient.ExportStateAsync();
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

        // Init waitEvent:
        _resetEventManager.Init("receive1");

        // Init Background
        await InitRegistryAndBackGroundServiceWorkerAsync();

        var senderMock = Substitute.For<ServiceBusSender>();
        _serviceBusClient
            .CreateSender(Arg.Any<string>())
            .Returns(senderMock);

        senderMock
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(async (callback) =>
            {
                var sb = callback.Arg<ServiceBusMessage>();
                var c = callback.Arg<CancellationToken>();

                _testOutputHelper.WriteLine("Sending Message from activity");

                await _worker.StartWorkerAsync("topicName", "subscriptionName", c);
                await _sbProcessorManager
                    .Get("topicName", "subscriptionName")
                    .SendMessage<dynamic>(new { hello = "world" }, null!);
            });


        // Start Workflow
        const string workflowDefinitionId = nameof(SendOneMessageWorkflow);
        var workflowClient = await CreateWorkflowClientAsync(workflowDefinitionId);
        var workflowState = await workflowClient.RunAsync(RunWorkflowInstanceRequest.Empty);

        /*
         * Workflow don't receive any message so it should be
         * Running
         * Suspended
         */
        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        //Wait for receiving first message
        var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait1 : {wait1}");

        await Task.Delay(500); //Todo find how to remove delay
        var lastWorkflowState = await workflowClient.ExportStateAsync();
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

        var senderMock = Substitute.For<ServiceBusSender>();
        _serviceBusClient
            .CreateSender(Arg.Any<string>())
            .Returns(senderMock);

        senderMock
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(async (callback) =>
            {
                var sb = callback.Arg<ServiceBusMessage>();
                var c = callback.Arg<CancellationToken>();

                _testOutputHelper.WriteLine("Sending Message from activity");

                await _worker.StartWorkerAsync("topicName", "subscriptionName", c);
                await _sbProcessorManager
                    .Get("topicName", "subscriptionName")
                    .SendMessage<dynamic>(new { hello = "world" }, correlationId);
            });

        //Start Workflow
        var workflowDefinitionId = nameof(SendOneMessageWithCorrelationIdWorkflow);
        var workflowClient = await CreateWorkflowClientAsync(workflowDefinitionId);
        var workflowState = await workflowClient.RunAsync(RunWorkflowInstanceRequest.Empty);

        /*
         * Workflow don't receive any message so it should be
         * Running
         * Suspended
         */
        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        // Wait for receiving first message.
        var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait1 : {wait1}");

        await Task.Delay(500); // Todo find how to remove delay
        var lastWorkflowState = await workflowClient.ExportStateAsync();
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

        // Init waitEvent:
        _resetEventManager.Init("receive1");
        _resetEventManager.Init("receive2");

        // Init backGround
        await InitRegistryAndBackGroundServiceWorkerAsync();

        // Start Workflow
        var workflowDefinitionId = nameof(ReceiveMessageWorkflow);
        var workflowClient = await CreateWorkflowClientAsync(workflowDefinitionId);
        var workflowState = await workflowClient.RunAsync(RunWorkflowInstanceRequest.Empty);

        /*
         * Workflow doesn't receive any message so it should be
         * Running
         * Suspended
         */
        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        //Start Worker to send Message on topicName/suscriptionName
        await _worker.StartWorkerAsync("topicName1", "subscriptionName1");
        await _sbProcessorManager
            .Get("topicName1", "subscriptionName1")
            .SendMessage<dynamic>(new { hello = "world" }, null!);

        //Wait for receiving first message
        var wait1 = _resetEventManager.Get("receive1").WaitOne(TimeSpan.FromSeconds(5));
        _testOutputHelper.WriteLine($"wait1 : {wait1}");

        var lastWorkflowState = await workflowClient.ExportStateAsync();
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
    
    private async Task InitRegistryAndBackGroundServiceWorkerAsync()
    {
        // Init Registries to use StartWorkflow
        await _services.PopulateRegistriesAsync();

        // Start background services for CommandHandler
        await _backgroundCommandSenderHostedService.StartAsync(CancellationToken.None);
        await _backgroundEventPublisherHostedService.StartAsync(CancellationToken.None);
    }
    
    private async Task<IWorkflowClient> CreateWorkflowClientAsync(string definitionId)
    {
        var workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        };
        await workflowClient.CreateInstanceAsync(createWorkflowInstanceRequest);
        return workflowClient;
    }

    void IDisposable.Dispose()
    {
        _backgroundCommandSenderHostedService.StopAsync(new CancellationToken()).GetAwaiter().GetResult();
    }
}