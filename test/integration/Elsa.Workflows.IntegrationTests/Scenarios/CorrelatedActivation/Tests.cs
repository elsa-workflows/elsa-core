using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
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
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);
        await using var scope1 = services.CreateAsyncScope();
        await using var scope2 = services.CreateAsyncScope();
        var starter1 = scope1.ServiceProvider.GetRequiredService<IWorkflowStarter>();
        var starter2 = scope2.ServiceProvider.GetRequiredService<IWorkflowStarter>();

        var results = await Task.WhenAll(
            starter1.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }),
            starter2.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }));

        Assert.Equal(1, results.Count(result => !result.CannotStart));
        Assert.Equal(1, results.Count(result => result.CannotStart));
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
        Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
    }

    [Fact(DisplayName = "Concurrent dispatch with the same CorrelationId and CorrelatedSingleton creates one Running instance")]
    public async Task ConcurrentDispatch_WithCorrelatedSingleton_CreatesOneRunningInstance()
    {
        var savedSignal = new WorkflowInstanceSavedSignal();
        var services = CreateServices(serviceCollection =>
        {
            serviceCollection.AddSingleton(savedSignal);
            serviceCollection.AddNotificationHandler<WorkflowInstanceSavedSignal, WorkflowInstanceSaved>(sp => sp.GetRequiredService<WorkflowInstanceSavedSignal>());
        });
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

            await savedSignal.Saved.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var instances = await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1");
            Assert.Single(instances);
            Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
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
        Assert.Equal(2, (await FindByCorrelationAsync(services, nameof(GroupedConversationWorkflow), "conversation-1")).Count);
    }

    [Fact(DisplayName = "CorrelatedSingleton requires a non-blank CorrelationId and persists no instance")]
    public async Task Start_WithBlankCorrelationId_ThrowsAndDoesNotPersistInstance()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = handle
        }));

        Assert.Contains("non-blank correlation ID", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await FindByDefinitionAsync(services, nameof(CorrelatedSingletonConversationWorkflow)));
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
        Assert.Single(await FindByCorrelationAsync(services, nameof(GlobalCorrelationConversationWorkflow), "shared-conversation"));
        Assert.Empty(await FindByCorrelationAsync(services, nameof(OtherGlobalCorrelationConversationWorkflow), "shared-conversation"));
    }

    [Fact(DisplayName = "Singleton scopes uniqueness to tenant and definition, independent of correlation ID")]
    public async Task Singleton_RefusesSecondRunningInstanceWithDifferentCorrelationId()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(SingletonConversationWorkflow), VersionOptions.Published);

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-1" });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-2" });

        Assert.False(first.CannotStart);
        Assert.True(second.CannotStart);
        Assert.Single(await FindByDefinitionAsync(services, nameof(SingletonConversationWorkflow)));
    }

    [Fact(DisplayName = "A terminal instance no longer occupies its activation scope")]
    public async Task CorrelatedSingleton_AllowsNewInstanceAfterPriorInstanceFinishes()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);
        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-terminal" });
        Assert.False(first.CannotStart);

        var firstInstance = Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal"));
        firstInstance.Status = WorkflowStatus.Finished;
        firstInstance.SubStatus = WorkflowSubStatus.Finished;
        firstInstance.WorkflowState.Status = WorkflowStatus.Finished;
        firstInstance.WorkflowState.SubStatus = WorkflowSubStatus.Finished;
        await services.GetRequiredService<IWorkflowInstanceStore>().SaveAsync(firstInstance);

        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-terminal" });

        Assert.False(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal"));
        Assert.Equal(2, (await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal")).Count);
    }

    private IServiceProvider CreateServices(Action<IServiceCollection>? configureServices = null)
    {
        var builder = new TestApplicationBuilder(_testOutputHelper);
        if (configureServices != null)
            builder.ConfigureServices(configureServices);

        return builder
            .AddWorkflow<CorrelatedSingletonConversationWorkflow>()
            .AddWorkflow<SingletonConversationWorkflow>()
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

    private static async Task<List<WorkflowInstance>> FindByCorrelationAsync(IServiceProvider services, string definitionId, string correlationId) =>
        (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            CorrelationId = correlationId
        })).ToList();

    private static async Task<List<WorkflowInstance>> FindByDefinitionAsync(IServiceProvider services, string definitionId) =>
        (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = definitionId
        })).ToList();

    private sealed class WorkflowInstanceSavedSignal : INotificationHandler<WorkflowInstanceSaved>
    {
        public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            Saved.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
