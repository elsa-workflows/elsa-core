using Elsa.Common.Models;
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

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionVersioning;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
    }

    [Test]
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

        var definitions = (await store.FindManyAsync(
            new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId
            }
        )).ToDictionary(x => x.Id);

        await Assert.That(definitions["v1"].IsLatest).IsFalse();
        await Assert.That(definitions["v2"].IsLatest).IsTrue();
        await Assert.That(definitions.Values.Where(x => x.IsLatest)).HasSingleItem();
        await Assert.That(definitions["v2"].Version).IsEqualTo(2);
        await Assert.That(savedDraft.Version).IsEqualTo(2);
    }

    [Test]
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

        var definitions = (await store.FindManyAsync(
            new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId
            }
        )).ToList();

        var persistedDraft = await Assert.That(definitions).HasSingleItem();

        await Assert.That(persistedDraft.Id).IsEqualTo("draft-1");
        await Assert.That(persistedDraft.Version).IsEqualTo(1);
        await Assert.That(persistedDraft.IsLatest).IsTrue();
        await Assert.That(savedDraft.Version).IsEqualTo(1);
        await Assert.That(savedDraft.IsLatest).IsTrue();
    }

    [Test]
    public async Task SaveDraftAsync_ShouldDemoteActualLatestWhenHighestVersionIsNotLatest()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        await store.SaveManyAsync([
            CreateDefinition("v1", definitionId, 1, true),
            CreateDefinition("v2", definitionId, 2, false)
        ]);

        var savedDraft = await publisher.SaveDraftAsync(CreateDefinition("v3", definitionId, 0, false));

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId })).ToList();
        await Assert.That(definitions.Single(x => x.Id == "v1").IsLatest).IsFalse();
        await Assert.That(definitions.Single(x => x.Id == "v2").IsLatest).IsFalse();
        await Assert.That(definitions.Single(x => x.Id == "v3").IsLatest).IsTrue();
        await Assert.That(savedDraft.Version).IsEqualTo(3);
        await Assert.That(definitions.Single(x => x.Id == "v3").Version).IsEqualTo(3);
        await Assert.That(definitions.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    public async Task SaveDraftAsync_ShouldPreserveActualLatestVersionWhenHigherVersionIsNotLatest()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var latestDraft = CreateDefinition("v1", definitionId, 1, true);
        await store.SaveManyAsync([
            latestDraft,
            CreateDefinition("v2", definitionId, 2, false)
        ]);

        var savedDraft = await publisher.SaveDraftAsync(latestDraft);

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId })).ToList();
        await Assert.That(definitions.Count).IsEqualTo(2);
        await Assert.That(savedDraft.Version).IsEqualTo(1);
        await Assert.That(definitions.Single(x => x.Id == "v1").Version).IsEqualTo(1);
        await Assert.That(definitions.Single(x => x.Id == "v1").IsLatest).IsTrue();
        await Assert.That(definitions.Single(x => x.Id == "v2").IsLatest).IsFalse();
        await Assert.That(definitions.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    public async Task SaveDraftAsync_ShouldPreserveHighestVersionWhenPromotingNonLatestDraft()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        await store.SaveManyAsync([
            CreateDefinition("v1", definitionId, 1, true),
            CreateDefinition("v2", definitionId, 2, false)
        ]);
        var highestDraft = await publisher.GetDraftAsync(definitionId, VersionOptions.SpecificVersion(2));

        var savedDraft = await publisher.SaveDraftAsync(highestDraft!);

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId })).ToList();
        await Assert.That(definitions.Count).IsEqualTo(2);
        await Assert.That(savedDraft.Id).IsEqualTo("v2");
        await Assert.That(savedDraft.Version).IsEqualTo(2);
        await Assert.That(definitions.Single(x => x.Id == "v1").IsLatest).IsFalse();
        await Assert.That(definitions.Single(x => x.Id == "v2").IsLatest).IsTrue();
        await Assert.That(definitions.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    public async Task SaveDraftAsync_ShouldPreservePublishedStateWhenCreatingDraftAfterPublishedLatest()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var publishedDefinition = CreateDefinition("v1", definitionId, 1, true);
        publishedDefinition.IsPublished = true;
        await store.SaveAsync(publishedDefinition);

        var savedDraft = await publisher.SaveDraftAsync(CreateDefinition("v2", definitionId, 0, false));

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId })).ToList();
        var persistedPublishedDefinition = definitions.Single(x => x.Id == "v1");
        await Assert.That(persistedPublishedDefinition.IsPublished).IsTrue();
        await Assert.That(persistedPublishedDefinition.IsLatest).IsFalse();
        await Assert.That(persistedPublishedDefinition.Version).IsEqualTo(1);
        await Assert.That(savedDraft.IsPublished).IsFalse();
        await Assert.That(savedDraft.IsLatest).IsTrue();
        await Assert.That(savedDraft.Version).IsEqualTo(2);
        await Assert.That(definitions.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    [Arguments(BatchFailurePoint.Replacement)]
    [Arguments(BatchFailurePoint.PreviousLatest)]
    public async Task SaveDraftAsync_WhenReplacementBatchFails_ShouldPreserveSinglePreviousLatest(
        BatchFailurePoint failurePoint
    )
    {
        const string definitionId = "test-definition";

        var persisted = new Dictionary<string, WorkflowDefinition>
        {
            ["v1"] = CreateDefinition("v1", definitionId, 1, true)
        };

        var store = CreateFailingStore(persisted, failurePoint);
        var publisher = ActivatorUtilities.CreateInstance<WorkflowDefinitionPublisher>(
            _services,
            store
        );

        var replacement = CreateDefinition("v2", definitionId, 0, false);

        await Assert.ThrowsExactlyAsync<TestPersistenceException>(
            () => publisher.SaveDraftAsync(replacement)
        );

        var previous = await Assert.That(persisted.Values).HasSingleItem();

        await Assert.That(previous.Id).IsEqualTo("v1");
        await Assert.That(previous.IsLatest).IsTrue();
        await Assert.That(persisted.Values.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    public async Task SaveDraftAsync_WhenDraftSavedNotificationFails_ShouldKeepCommittedLatestStateConsistent()
    {
        const string definitionId = "test-definition";

        await using var services = (ServiceProvider)new TestApplicationBuilder(
            _services.GetRequiredService<TextWriter>()
        )
            .ConfigureServices(serviceCollection =>
                serviceCollection.AddNotificationHandler<
                    FailingDraftSavedHandler,
                    WorkflowDefinitionDraftSaved
                >()
            )
            .Build();

        var store = services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();

        await store.SaveAsync(CreateDefinition("v1", definitionId, 1, true));

        await Assert.ThrowsExactlyAsync<TestNotificationException>(
            () => publisher.SaveDraftAsync(CreateDefinition("v2", definitionId, 0, false))
        );

        var definitions = (await store.FindManyAsync(
            new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId
            }
        )).ToList();

        await Assert.That(definitions.Count).IsEqualTo(2);
        await Assert.That(definitions.Single(x => x.Id == "v1").IsLatest).IsFalse();
        await Assert.That(definitions.Single(x => x.Id == "v2").IsLatest).IsTrue();
        await Assert.That(definitions.Where(x => x.IsLatest)).HasSingleItem();
    }

    [Test]
    public async Task RevertVersionAsync_ShouldAllocateVersionAfterHighestStoredVersion()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();

        var definitions = new WorkflowDefinition[]
        {
            new()
            {
                Id = "v1",
                DefinitionId = definitionId,
                Version = 1,
                IsPublished = true,
                IsLatest = false
            },
            new()
            {
                Id = "v2",
                DefinitionId = definitionId,
                Version = 2,
                IsPublished = false,
                IsLatest = true
            },
            new()
            {
                Id = "v3",
                DefinitionId = definitionId,
                Version = 3,
                IsPublished = false,
                IsLatest = false
            }
        };

        await store.SaveManyAsync(definitions);

        var revertedDefinition = await publisher.RevertVersionAsync(definitionId, 1);

        await Assert.That(revertedDefinition.Version).IsEqualTo(4);
    }

    private static IWorkflowDefinitionStore CreateFailingStore(
        IDictionary<string, WorkflowDefinition> persisted,
        BatchFailurePoint failurePoint
    )
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();

        store.FindLastVersionAsync(
                Arg.Any<WorkflowDefinitionFilter>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
                Task.FromResult(
                    persisted.Values
                        .OrderByDescending(x => x.Version)
                        .Select(Clone)
                        .FirstOrDefault()
                )
            );

        store.FindAsync(
                Arg.Is<WorkflowDefinitionFilter>(x => x.VersionOptions!.Value.IsLatest),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
                Task.FromResult(
                    persisted.Values
                        .Where(x => x.IsLatest)
                        .Select(Clone)
                        .FirstOrDefault()
                )
            );

        store.SaveManyAsync(
                Arg.Any<IEnumerable<WorkflowDefinition>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo =>
            {
                var definitions = callInfo.Arg<IEnumerable<WorkflowDefinition>>().ToList();

                var snapshot = persisted.ToDictionary(
                    x => x.Key,
                    x => Clone(x.Value)
                );

                try
                {
                    foreach (var definition in definitions)
                    {
                        if (
                            failurePoint == BatchFailurePoint.PreviousLatest
                            && definition.Id == "v1"
                        )
                        {
                            throw new TestPersistenceException();
                        }

                        if (
                            failurePoint == BatchFailurePoint.Replacement
                            && definition.Id == "v2"
                        )
                        {
                            throw new TestPersistenceException();
                        }

                        persisted[definition.Id] = Clone(definition);
                    }
                }
                catch
                {
                    persisted.Clear();

                    foreach (var item in snapshot)
                    {
                        persisted[item.Key] = item.Value;
                    }

                    throw;
                }

                return Task.CompletedTask;
            });

        return store;
    }

    private static WorkflowDefinition CreateDefinition(
        string id,
        string definitionId,
        int version,
        bool isLatest
    ) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Version = version,
            IsLatest = isLatest,
            IsPublished = false
        };

    private static WorkflowDefinition Clone(WorkflowDefinition definition) =>
        definition.ShallowClone();

    public enum BatchFailurePoint
    {
        Replacement,
        PreviousLatest
    }

    private class FailingDraftSavedHandler
        : INotificationHandler<WorkflowDefinitionDraftSaved>
    {
        public Task HandleAsync(
            WorkflowDefinitionDraftSaved notification,
            CancellationToken cancellationToken
        ) => throw new TestNotificationException();
    }

    private class TestPersistenceException : Exception;

    private class TestNotificationException : Exception;

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
