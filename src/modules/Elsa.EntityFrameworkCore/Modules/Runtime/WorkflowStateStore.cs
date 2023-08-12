using System.Text.Json.Serialization;
using Elsa.Common.Contracts;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowStateStore : IWorkflowStateStore
{
    private readonly ISystemClock _systemClock;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowState> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowStateStore(
        EntityStore<RuntimeElsaDbContext, WorkflowState> store,
        IWorkflowStateSerializer workflowStateSerializer,
        ISystemClock systemClock)
    {
        _systemClock = systemClock;
        _workflowStateSerializer = workflowStateSerializer;
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(state, OnSaveAsync, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private async ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, WorkflowState entity, CancellationToken cancellationToken)
    {
        var state = new WorkflowStateState(entity.Bookmarks, entity.CompletionCallbacks, entity.ActivityExecutionContexts, entity.Output, entity.Properties);
        var json = await _workflowStateSerializer.SerializeAsync(state, cancellationToken);
        var now = _systemClock.UtcNow;
        var entry = dbContext.Entry(entity);

        entity.CreatedAt = entity.CreatedAt == default ? now : entity.CreatedAt;
        entity.UpdatedAt = now;

        entry.Property<string>("Data").CurrentValue = json;
    }

    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, WorkflowState? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        var entry = dbContext.Entry(entity);
        var json = entry.Property<string>("Data").CurrentValue;
        var data = await _workflowStateSerializer.DeserializeAsync<WorkflowStateState>(json, cancellationToken);

        entity.Bookmarks = data.Bookmarks;
        entity.CompletionCallbacks = data.CompletionCallbacks;
        entity.ActivityExecutionContexts = data.ActivityExecutionContexts;
        entity.Output = data.Output;
        entity.Properties = data.Properties;
    }

    private class WorkflowStateState
    {
        [JsonConstructor]
        public WorkflowStateState()
        {
        }

        public WorkflowStateState(
            ICollection<Bookmark> bookmarks,
            ICollection<CompletionCallbackState> completionCallbacks,
            ICollection<ActivityExecutionContextState> activityExecutionContexts,
            IDictionary<string, object> output,
            IDictionary<string, object> properties
        )
        {
            Bookmarks = bookmarks;
            CompletionCallbacks = completionCallbacks;
            ActivityExecutionContexts = activityExecutionContexts;
            Output = output;
            Properties = properties;
        }

        public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public ICollection<CompletionCallbackState> CompletionCallbacks { get; set; } = new List<CompletionCallbackState>();
        public ICollection<ActivityExecutionContextState> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContextState>();
        public IDictionary<string, object> Output { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}