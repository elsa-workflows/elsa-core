using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionVersioning;

public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact]
    public async Task SaveDraftAsync_ShouldClearLatestFlagFromPreviousUnpublishedDraft()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var existingDraft = new WorkflowDefinition
        {
            Id = "v1",
            DefinitionId = definitionId,
            Version = 1,
            IsPublished = false,
            IsLatest = true
        };
        var newDraft = new WorkflowDefinition
        {
            Id = "v2",
            DefinitionId = definitionId
        };

        await store.SaveAsync(existingDraft);

        var savedDraft = await publisher.SaveDraftAsync(newDraft);

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId
        })).ToDictionary(x => x.Id);
        Assert.False(definitions["v1"].IsLatest);
        Assert.True(definitions["v2"].IsLatest);
        Assert.Single(definitions.Values, x => x.IsLatest);
        Assert.Equal(2, definitions["v2"].Version);
        Assert.Equal(2, savedDraft.Version);
    }

    [Fact]
    public async Task SaveDraftAsync_ShouldKeepExistingDraftLatestWhenResaved()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var existingDraft = new WorkflowDefinition
        {
            Id = "draft-1",
            DefinitionId = definitionId,
            Version = 1,
            IsPublished = false,
            IsLatest = true
        };

        await store.SaveAsync(existingDraft);

        var savedDraft = await publisher.SaveDraftAsync(existingDraft);

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId
        })).ToList();
        var persistedDraft = Assert.Single(definitions);
        Assert.Equal("draft-1", persistedDraft.Id);
        Assert.Equal(1, persistedDraft.Version);
        Assert.True(persistedDraft.IsLatest);
        Assert.Equal(1, savedDraft.Version);
        Assert.True(savedDraft.IsLatest);
    }

    [Theory]
    [InlineData(BatchFailurePoint.Replacement)]
    [InlineData(BatchFailurePoint.PreviousLatest)]
    public async Task SaveDraftAsync_WhenReplacementBatchFails_ShouldPreserveSinglePreviousLatest(BatchFailurePoint failurePoint)
    {
        const string definitionId = "test-definition";
        var persisted = new Dictionary<string, WorkflowDefinition>
        {
            ["v1"] = CreateDefinition("v1", definitionId, 1, true)
        };
        var store = CreateFailingStore(persisted, failurePoint);
        var publisher = ActivatorUtilities.CreateInstance<WorkflowDefinitionPublisher>(_services, store);
        var replacement = CreateDefinition("v2", definitionId, 0, false);

        await Assert.ThrowsAsync<TestPersistenceException>(() => publisher.SaveDraftAsync(replacement));

        var previous = Assert.Single(persisted.Values);
        Assert.Equal("v1", previous.Id);
        Assert.True(previous.IsLatest);
        Assert.Single(persisted.Values, x => x.IsLatest);
    }

    [Fact]
    public async Task SaveDraftAsync_WhenDraftSavedNotificationFails_ShouldKeepCommittedLatestStateConsistent()
    {
        const string definitionId = "test-definition";
        var services = new TestApplicationBuilder(_services.GetRequiredService<ITestOutputHelper>())
            .ConfigureServices(serviceCollection => serviceCollection.AddNotificationHandler<FailingDraftSavedHandler, WorkflowDefinitionDraftSaved>())
            .Build();
        var store = services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();
        await store.SaveAsync(CreateDefinition("v1", definitionId, 1, true));

        await Assert.ThrowsAsync<TestNotificationException>(() => publisher.SaveDraftAsync(CreateDefinition("v2", definitionId, 0, false)));

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId })).ToList();
        Assert.Equal(2, definitions.Count);
        Assert.False(definitions.Single(x => x.Id == "v1").IsLatest);
        Assert.True(definitions.Single(x => x.Id == "v2").IsLatest);
        Assert.Single(definitions, x => x.IsLatest);
    }

    private static IWorkflowDefinitionStore CreateFailingStore(
        IDictionary<string, WorkflowDefinition> persisted,
        BatchFailurePoint failurePoint)
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();
        store.FindLastVersionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(persisted.Values.OrderByDescending(x => x.Version).Select(Clone).FirstOrDefault()));
        store.SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var definitions = callInfo.Arg<IEnumerable<WorkflowDefinition>>().ToList();
                var snapshot = persisted.ToDictionary(x => x.Key, x => Clone(x.Value));

                try
                {
                    foreach (var definition in definitions)
                    {
                        if (failurePoint == BatchFailurePoint.PreviousLatest && definition.Id == "v1")
                            throw new TestPersistenceException();
                        if (failurePoint == BatchFailurePoint.Replacement && definition.Id == "v2")
                            throw new TestPersistenceException();

                        persisted[definition.Id] = Clone(definition);
                    }
                }
                catch
                {
                    persisted.Clear();
                    foreach (var item in snapshot)
                        persisted[item.Key] = item.Value;
                    throw;
                }

                return Task.CompletedTask;
            });
        return store;
    }

    private static WorkflowDefinition CreateDefinition(string id, string definitionId, int version, bool isLatest) => new()
    {
        Id = id,
        DefinitionId = definitionId,
        Version = version,
        IsLatest = isLatest,
        IsPublished = false
    };

    private static WorkflowDefinition Clone(WorkflowDefinition definition) => definition.ShallowClone();

    public enum BatchFailurePoint
    {
        Replacement,
        PreviousLatest
    }

    private class FailingDraftSavedHandler : INotificationHandler<WorkflowDefinitionDraftSaved>
    {
        public Task HandleAsync(WorkflowDefinitionDraftSaved notification, CancellationToken cancellationToken) => throw new TestNotificationException();
    }

    private class TestPersistenceException : Exception;
    private class TestNotificationException : Exception;
}
