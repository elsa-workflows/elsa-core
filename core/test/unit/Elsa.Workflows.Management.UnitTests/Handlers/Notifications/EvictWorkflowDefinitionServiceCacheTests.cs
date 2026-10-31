using Elsa.Workflows.Management.Handlers.Notifications;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Handlers.Notifications;

/// <summary>
/// Each completed write (publish, retract, delete, bulk delete) must evict the definition again, so a read that raced the write cannot leave a stale entry.
/// </summary>
public class EvictWorkflowDefinitionServiceCacheTests
{
    private readonly IWorkflowDefinitionCacheManager _cacheManager = Substitute.For<IWorkflowDefinitionCacheManager>();
    private readonly EvictWorkflowDefinitionServiceCache _handler;

    public EvictWorkflowDefinitionServiceCacheTests()
    {
        _handler = new(_cacheManager);
    }

    [Fact]
    public async Task Published_EvictsDefinition()
    {
        await _handler.HandleAsync(new WorkflowDefinitionPublished(Definition("wf-1"), new AffectedWorkflows(new List<WorkflowDefinition>())), CancellationToken.None);

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retracted_EvictsDefinition()
    {
        await _handler.HandleAsync(new WorkflowDefinitionRetracted(Definition("wf-1")), CancellationToken.None);

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_EvictsDefinition()
    {
        await _handler.HandleAsync(new WorkflowDefinitionDeleted("wf-1"), CancellationToken.None);

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleted_EvictsEachDefinition()
    {
        await _handler.HandleAsync(new WorkflowDefinitionsDeleted(["wf-1", "wf-2"]), CancellationToken.None);

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-2", Arg.Any<CancellationToken>());
    }

    private static WorkflowDefinition Definition(string definitionId) => new() { DefinitionId = definitionId };
}
