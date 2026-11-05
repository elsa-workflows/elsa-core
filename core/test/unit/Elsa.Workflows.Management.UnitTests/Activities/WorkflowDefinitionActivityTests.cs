using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ChildReferenceActivity = Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity.WorkflowDefinitionActivity;

namespace Elsa.Workflows.Management.UnitTests.Activities;

public class WorkflowDefinitionActivityTests
{
    private const string ChildDefinitionId = "child";
    private const string ExportedChildVersionId = "child-v1";

    private readonly TestTenantAccessor _tenantAccessor = new("tenant-a");
    private readonly MemoryWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionService _definitionService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IServiceProvider _serviceProvider;
    private int _generatedIds;

    public WorkflowDefinitionActivityTests()
    {
        _store = new(new MemoryStore<WorkflowDefinition>(), _tenantAccessor);
        _definitionService.FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(call => _store.FindAsync(call.Arg<WorkflowDefinitionFilter>(), call.Arg<CancellationToken>()));
        _serviceProvider = new ServiceCollection().AddSingleton(_definitionService).BuildServiceProvider();
    }

    [Fact(DisplayName = "A pinned child resolves by its version number in a tenant that imported it under a fresh version id")]
    public async Task GetWorkflowDefinitionAsync_VersionIdMissesAfterCrossTenantImport_ResolvesPinnedVersion()
    {
        await ImportChildAsync("tenant-a");
        await ImportChildAsync("tenant-b");
        await PublishNewerChildVersionAsync("tenant-b");

        var child = await ResolveChildAsync("tenant-b", version: 1);

        Assert.NotNull(child);
        Assert.Equal(1, child.Version);
        Assert.Equal("tenant-b", child.TenantId);
        Assert.NotEqual(ExportedChildVersionId, child.Id);
    }

    [Fact(DisplayName = "A pinned child still falls back to the published version when the pinned version number is missing")]
    public async Task GetWorkflowDefinitionAsync_PinnedVersionMissing_FallsBackToPublished()
    {
        await ImportChildAsync("tenant-a");
        await ImportChildAsync("tenant-b");
        await PublishNewerChildVersionAsync("tenant-b");

        var child = await ResolveChildAsync("tenant-b", version: 3);

        Assert.Equal(2, child?.Version);
    }

    [Fact(DisplayName = "A pinned version number that is an open draft is not run in place of the published version")]
    public async Task GetWorkflowDefinitionAsync_PinnedVersionIsOpenDraft_FallsBackToPublished()
    {
        using (_tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            await _store.SaveManyAsync(
            [
                new WorkflowDefinition { Id = "tenant-b-child-v1", DefinitionId = ChildDefinitionId, Version = 1, TenantId = "tenant-b", IsPublished = true, MaterializerName = "Json" },
                new WorkflowDefinition { Id = "tenant-b-child-v2", DefinitionId = ChildDefinitionId, Version = 2, TenantId = "tenant-b", IsLatest = true, MaterializerName = "Json" }
            ]);
        }

        var child = await ResolveChildAsync("tenant-b", version: 2);

        Assert.Equal(1, child?.Version);
    }

    [Fact(DisplayName = "A pinned version id that exists wins over a different pinned version number")]
    public async Task GetWorkflowDefinitionAsync_VersionIdHit_WinsOverPinnedVersionNumber()
    {
        await ImportChildAsync("tenant-a");
        await PublishNewerChildVersionAsync("tenant-a");

        var child = await ResolveChildAsync("tenant-a", version: 2);

        Assert.Equal(ExportedChildVersionId, child?.Id);
        Assert.Equal(1, child?.Version);
    }

    private async Task<WorkflowDefinition?> ResolveChildAsync(string tenantId, int version)
    {
        var activity = new ChildReferenceActivity
        {
            WorkflowDefinitionId = ChildDefinitionId,
            WorkflowDefinitionVersionId = ExportedChildVersionId,
            Version = version
        };

        using (_tenantAccessor.PushContext(new Tenant { Id = tenantId }))
            return await activity.GetWorkflowDefinitionAsync(_serviceProvider, CancellationToken.None);
    }

    // The importer always starts a new definition at version 1, so the pinned number lines up only when the child was exported at version 1.
    private async Task ImportChildAsync(string tenantId)
    {
        var publisher = Substitute.For<IWorkflowDefinitionPublisher>();
        publisher.GetDraftAsync(Arg.Any<string>(), Arg.Any<VersionOptions>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        publisher.NewAsync(Arg.Any<IActivity?>(), Arg.Any<CancellationToken>()).Returns(_ => new WorkflowDefinition { Id = $"generated-{++_generatedIds}", Version = 1, IsLatest = true });
        publisher.SaveDraftAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var draft = call.Arg<WorkflowDefinition>();
            draft.TenantId = tenantId;
            await _store.SaveAsync(draft);
            return draft;
        });
        var serializer = Substitute.For<IActivitySerializer>();
        serializer.Serialize(Arg.Any<IActivity>()).Returns("{}");
        var variableMapper = new VariableDefinitionMapper(SerializationTypeRegistry.CreateDefault(), Substitute.For<IServiceScopeFactory>(), NullLogger<VariableDefinitionMapper>.Instance);
        var importer = new WorkflowDefinitionImporter(serializer, publisher, variableMapper, _store);

        using (_tenantAccessor.PushContext(new Tenant { Id = tenantId }))
        {
            await importer.ImportAsync(new()
            {
                Model = new WorkflowDefinitionModel { Id = ExportedChildVersionId, DefinitionId = ChildDefinitionId, Name = "Child" },
                Publish = false
            });
        }
    }

    private async Task PublishNewerChildVersionAsync(string tenantId)
    {
        using (_tenantAccessor.PushContext(new Tenant { Id = tenantId }))
        {
            var first = (await _store.FindAsync(new WorkflowDefinitionFilter { DefinitionId = ChildDefinitionId, VersionOptions = VersionOptions.SpecificVersion(1) }))!;
            first.IsLatest = false;
            await _store.SaveManyAsync(
            [
                first,
                new WorkflowDefinition { Id = $"{tenantId}-child-v2", DefinitionId = ChildDefinitionId, Version = 2, TenantId = tenantId, IsLatest = true, IsPublished = true, MaterializerName = "Json" }
            ]);
        }
    }
}
