using Elsa.Common.Models;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CorrelatedActivation;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "Concurrent start with the same CorrelationId and CorrelatedSingleton creates one Running instance")]
    public async Task ConcurrentStart_WithCorrelatedSingleton_CreatesOneRunningInstance()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);

        var results = await Task.WhenAll(
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }),
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }));

        Assert.Equal(1, results.Count(result => !result.CannotStart));
        Assert.Equal(1, results.Count(result => result.CannotStart));
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
    }

    [Fact(DisplayName = "Concurrent dispatch with the same CorrelationId and CorrelatedSingleton creates one Running instance")]
    public async Task ConcurrentDispatch_WithCorrelatedSingleton_CreatesOneRunningInstance()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var graph = await FindGraphAsync(services, nameof(CorrelatedSingletonConversationWorkflow));
        var dispatcher = services.GetRequiredService<IWorkflowDispatcher>();
        var commandProcessor = services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await commandProcessor.StartAsync(CancellationToken.None);

        try
        {
            await Task.WhenAll(
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id)
                {
                    CorrelationId = "conversation-1"
                }, null),
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id)
                {
                    CorrelationId = "conversation-1"
                }, null));

            await WaitUntilAsync(async () => (await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1")).Count != 0);

            var instances = await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1");
            Assert.Single(instances);
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    [Fact(DisplayName = "Without an activation strategy, the same CorrelationId may have many Running instances")]
    public async Task ConcurrentStart_WithoutStrategy_AllowsMultipleRunningInstances()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(GroupedConversationWorkflow), VersionOptions.Published);

        var results = await Task.WhenAll(
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }),
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }));

        Assert.All(results, result => Assert.False(result.CannotStart));
        Assert.Equal(2, (await FindRunningAsync(services, nameof(GroupedConversationWorkflow), "conversation-1")).Count);
    }

    [Fact(DisplayName = "Blank CorrelationId with CorrelatedSingleton still allows many Running instances")]
    public async Task ConcurrentStart_WithBlankCorrelationId_AllowsMultipleRunningInstances()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);

        var results = await Task.WhenAll(
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle
            }),
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle
            }));

        Assert.All(results, result => Assert.False(result.CannotStart));
        Assert.Equal(2, (await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), correlationId: null)).Count);
    }

    [Fact(DisplayName = "CorrelatedSingleton scopes uniqueness to DefinitionId + CorrelationId")]
    public async Task CorrelatedSingleton_AllowsSameCorrelationIdOnDifferentDefinitions()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(OtherCorrelatedSingletonConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });

        Assert.False(first.CannotStart);
        Assert.False(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "shared-conversation"));
        Assert.Single(await FindRunningAsync(services, nameof(OtherCorrelatedSingletonConversationWorkflow), "shared-conversation"));
    }

    [Fact(DisplayName = "Correlation strategy refuses a second Running instance for the same CorrelationId across definitions")]
    public async Task CorrelationStrategy_RefusesSecondDefinitionWithSameCorrelationId()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(GlobalCorrelationConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(OtherGlobalCorrelationConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });

        Assert.False(first.CannotStart);
        Assert.True(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(GlobalCorrelationConversationWorkflow), "shared-conversation"));
        Assert.Empty(await FindRunningAsync(services, nameof(OtherGlobalCorrelationConversationWorkflow), "shared-conversation"));
    }

    private IServiceProvider CreateServices()
    {
        return new TestApplicationBuilder(_testOutputHelper)
            .AddWorkflow<CorrelatedSingletonConversationWorkflow>()
            .AddWorkflow<GroupedConversationWorkflow>()
            .AddWorkflow<OtherCorrelatedSingletonConversationWorkflow>()
            .AddWorkflow<GlobalCorrelationConversationWorkflow>()
            .AddWorkflow<OtherGlobalCorrelationConversationWorkflow>()
            .Build();
    }

    private static async Task<WorkflowGraph> FindGraphAsync(IServiceProvider services, string definitionId)
    {
        var graph = await services.GetRequiredService<IWorkflowDefinitionService>()
            .FindWorkflowGraphAsync(definitionId, VersionOptions.Published);
        Assert.NotNull(graph);
        return graph;
    }

    private static async Task<List<WorkflowInstance>> FindRunningAsync(IServiceProvider services, string definitionId, string? correlationId)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            WorkflowStatus = WorkflowStatus.Running
        };

        if (correlationId != null)
            filter.CorrelationId = correlationId;

        return (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(filter)).ToList();
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeout.IsCancellationRequested)
        {
            if (await predicate())
                return;

            await Task.Delay(50, CancellationToken.None);
        }

        Assert.Fail("Timed out waiting for a workflow instance to appear.");
    }
}
