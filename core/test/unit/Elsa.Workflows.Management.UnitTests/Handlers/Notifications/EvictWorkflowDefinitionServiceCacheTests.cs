using Elsa.Mediator.Contracts;
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

    [Fact]
    public async Task Published_EvictsDefinition()
    {
        await HandleAsync(new WorkflowDefinitionPublished(Definition("wf-1"), new AffectedWorkflows(new List<WorkflowDefinition>())));

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retracted_EvictsDefinition()
    {
        await HandleAsync(new WorkflowDefinitionRetracted(Definition("wf-1")));

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_EvictsDefinition()
    {
        await HandleAsync(new WorkflowDefinitionDeleted("wf-1"));

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleted_EvictsEachDefinition()
    {
        await HandleAsync(new WorkflowDefinitionsDeleted(["wf-1", "wf-2"]));

        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-1", Arg.Any<CancellationToken>());
        await _cacheManager.Received(1).EvictWorkflowDefinitionAsync("wf-2", Arg.Any<CancellationToken>());
    }

    private static WorkflowDefinition Definition(string definitionId) => new() { DefinitionId = definitionId };

    // The handler is internal, so create it by its type name and call it through the public handler interface.
    private Task HandleAsync<TNotification>(TNotification notification) where TNotification : INotification
    {
        var handlerType = typeof(IWorkflowDefinitionCacheManager).Assembly.GetType("Elsa.Workflows.Management.Handlers.Notifications.EvictWorkflowDefinitionServiceCache", throwOnError: true)!;
        var handler = (INotificationHandler<TNotification>)Activator.CreateInstance(handlerType, _cacheManager)!;
        return handler.HandleAsync(notification, CancellationToken.None);
    }
}
