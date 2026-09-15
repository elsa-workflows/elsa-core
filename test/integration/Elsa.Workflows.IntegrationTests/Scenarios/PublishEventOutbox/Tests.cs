using Elsa.Common.Models;
using Elsa.KeyValues.Features;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.PublishEventOutbox;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "With outbox on, in-workflow PublishEvent survives a crash between parent commit and delivery")]
    public async Task PublishEvent_WithOutboxOn_SurvivesCrashBetweenCommitAndDelivery()
    {
        var services = CreateServices(useTransactionalOutbox: true, processOutboxAfterCommit: false);
        await services.PopulateRegistriesAsync();

        var parentResponse = await RunPublisherAsync(services);
        Assert.Equal(WorkflowStatus.Finished, parentResponse.Status);

        var outboxItems = (await services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync()).ToList();
        var outboxItem = Assert.Single(outboxItems);
        Assert.Equal(WorkflowDispatchOutboxItemKind.TriggerWorkflows, outboxItem.Kind);
        Assert.Equal(parentResponse.WorkflowInstanceId, outboxItem.OwnerWorkflowInstanceId);
        Assert.NotNull(outboxItem.TriggerWorkflowsCommand);
        Assert.Equal(ActivityTypeNameHelper.GenerateTypeName<Event>(), outboxItem.TriggerWorkflowsCommand.ActivityTypeName);
        Assert.Equal(PublishOrderShippedEventWorkflow.EventName, Assert.IsType<EventStimulus>(outboxItem.TriggerWorkflowsCommand.Stimulus).EventName);

        var parentInstance = await services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter
        {
            Id = parentResponse.WorkflowInstanceId
        });
        Assert.NotNull(parentInstance);
        Assert.True(parentInstance.WorkflowState.HasWorkflowDispatchOutboxItem(outboxItem.Id));
        Assert.Empty(await FindConsumerInstancesAsync(services));

        await services.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        Assert.Empty(await services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync());

        var commandProcessor = services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await commandProcessor.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilConsumerInstanceExistsAsync(services);
            Assert.Single(await FindConsumerInstancesAsync(services));
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    [Fact(DisplayName = "With outbox off, in-workflow PublishEvent is not written to the outbox")]
    public async Task PublishEvent_WithOutboxOff_DoesNotWriteToOutbox()
    {
        var services = CreateServices(useTransactionalOutbox: false, processOutboxAfterCommit: false);
        await services.PopulateRegistriesAsync();

        var parentResponse = await RunPublisherAsync(services);
        Assert.Equal(WorkflowStatus.Finished, parentResponse.Status);
        Assert.Empty(await services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync());
    }

    private IServiceProvider CreateServices(bool useTransactionalOutbox, bool processOutboxAfterCommit)
    {
        return new TestApplicationBuilder(_testOutputHelper)
            .AddWorkflow<PublishOrderShippedEventWorkflow>()
            .AddWorkflow<ConsumeOrderShippedEventWorkflow>()
            .ConfigureElsa(elsa => elsa.Configure<KeyValueFeature>())
            .ConfigureServices(services => services.Configure<WorkflowDispatcherOptions>(options =>
            {
                options.UseTransactionalOutbox = useTransactionalOutbox;
                options.ProcessOutboxAfterCommit = processOutboxAfterCommit;
            }))
            .Build();
    }

    private static async Task<RunWorkflowInstanceResponse> RunPublisherAsync(IServiceProvider services)
    {
        var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        return await workflowClient.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(PublishOrderShippedEventWorkflow), VersionOptions.Published)
        });
    }

    private static ValueTask<IEnumerable<WorkflowInstance>> FindConsumerInstancesAsync(IServiceProvider services)
    {
        return services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = nameof(ConsumeOrderShippedEventWorkflow)
        });
    }

    private static async Task WaitUntilConsumerInstanceExistsAsync(IServiceProvider services)
    {
        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeoutTokenSource.IsCancellationRequested)
        {
            if ((await FindConsumerInstancesAsync(services)).Any())
                return;

            await Task.Delay(50);
        }

        Assert.Fail("ConsumeOrderShippedEventWorkflow was not created before the timeout elapsed.");
    }
}
