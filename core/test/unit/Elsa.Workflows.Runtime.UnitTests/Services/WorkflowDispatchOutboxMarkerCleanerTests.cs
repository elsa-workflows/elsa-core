using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Features.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ModuleWorkflowRuntimeFeature = Elsa.Workflows.Runtime.Features.WorkflowRuntimeFeature;
using ShellWorkflowRuntimeFeature = Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowDispatchOutboxMarkerCleanerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoveAsync_DoesNotSave_WhenOwnerOrMarkerIsMissing(bool ownerExists)
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == "parent-1"), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(ownerExists ? CreateOwner() : null));
        var cleaner = new WorkflowDispatchOutboxMarkerCleaner(store, new NoopDistributedSynchronizationProvider(), Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()));

        await cleaner.RemoveAsync(CreateItem());

        await store.Received(1).FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == "parent-1"), Arg.Any<CancellationToken>());
        await store.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Theory]
    [InlineData(false, "default")]
    [InlineData(true, "default")]
    [InlineData(false, "before")]
    [InlineData(true, "before")]
    [InlineData(false, "after")]
    [InlineData(true, "after")]
    public async Task FeatureRegistration_UsesConfiguredCleanup_ForModuleAndShell(bool shell, string registration)
    {
        var services = new ServiceCollection();
        var customCleaner = Substitute.For<IWorkflowDispatchOutboxMarkerCleaner>();
        if (registration == "before")
        {
            services.AddScoped<IWorkflowDispatchOutboxMarkerCleaner>(_ => customCleaner);
        }

        if (shell)
        {
            new ShellWorkflowRuntimeFeature().ConfigureServices(services);
        }
        else
        {
            var module = Substitute.For<IModule>();
            module.Services.Returns(services);
            new ModuleWorkflowRuntimeFeature(module).Apply();
        }

        if (registration == "after")
        {
            services.Replace(ServiceDescriptor.Scoped<IWorkflowDispatchOutboxMarkerCleaner>(_ => customCleaner));
        }

        var item = CreateItem();
        var owner = CreateOwner();
        owner.WorkflowState.Properties[WorkflowDispatchOutboxStateExtensions.PropertyKey] = new WorkflowDispatchOutboxState { ItemIds = [item.Id] };
        var outboxStore = Substitute.For<IWorkflowDispatchOutboxStore>();
        var instanceStore = Substitute.For<IWorkflowInstanceStore>();
        var dispatcher = Substitute.For<IWorkflowDispatcher>();
        outboxStore.FindManyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        instanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == owner.Id), Arg.Any<CancellationToken>()).Returns(new ValueTask<WorkflowInstance?>(owner));
        dispatcher.DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions?>(), Arg.Any<CancellationToken>()).Returns(DispatchWorkflowResponse.Success());
        services.AddLogging();
        services.AddSingleton(outboxStore);
        services.AddSingleton(instanceStore);
        services.AddSingleton(Substitute.For<ISystemClock>());
        services.AddSingleton<IDistributedLockProvider>(new NoopDistributedSynchronizationProvider());
        services.AddKeyedSingleton<IWorkflowDispatcher>(WorkflowDispatcherServiceKeys.Inner, dispatcher);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IWorkflowDispatchOutboxProcessor>().ProcessAsync();

        await outboxStore.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
        if (registration == "default")
        {
            Assert.False(owner.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id));
            await instanceStore.Received(1).SaveAsync(owner, Arg.Any<CancellationToken>());
        }
        else
        {
            await customCleaner.Received(1).RemoveAsync(item, Arg.Any<CancellationToken>());
            await instanceStore.Received(1).FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>());
            await instanceStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        }
    }

    private static WorkflowDispatchOutboxItem CreateItem() => new()
    {
        Id = "outbox-1",
        OwnerWorkflowInstanceId = "parent-1",
        Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
        WorkflowDefinitionCommand = new DispatchWorkflowDefinitionCommand("definition-version-1") { InstanceId = "child-1" },
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static WorkflowInstance CreateOwner() => new()
    {
        Id = "parent-1",
        WorkflowState = new WorkflowState { Id = "parent-1", Properties = new Dictionary<string, object>() }
    };
}