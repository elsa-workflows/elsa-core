using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Management.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Management.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
[UsedImplicitly]
internal class DapperWorkflowInstanceStore(Store<WorkflowInstanceRecord> store, IWorkflowStateSerializer workflowStateSerializer)
    : IWorkflowInstanceStore
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Dapper.Modules.Management.Stores.DapperWorkflowInstanceStore.MapAsync(WorkflowInstanceRecord)")]
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Dapper.Modules.Management.Stores.DapperWorkflowInstanceStore.FindManyAsync<TOrderBy>(WorkflowInstanceFilter, PageArgs, WorkflowInstanceOrder<TOrderBy>, CancellationToken)")]
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Dapper.Modules.Management.Stores.DapperWorkflowInstanceStore.MapAsync(Page<WorkflowInstanceRecord>)")]
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Dapper.Modules.Management.Stores.DapperWorkflowInstanceStore.MapAsync(IEnumerable<WorkflowInstanceRecord>)")]
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Dapper.Modules.Management.Stores.DapperWorkflowInstanceStore.MapAsync(IEnumerable<WorkflowInstanceRecord>)")]
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.CountAsync(query => ApplyFilter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await SummarizeManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync<WorkflowInstanceSummary>(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await SummarizeManyAsync(filter, new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync<WorkflowInstanceSummary>(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var items = await store.FindManyAsync<WorkflowInstanceId>(q => ApplyFilter(q, filter), cancellationToken);
        return items.Select(x => x.Id).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyIdsAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await store.FindManyAsync<WorkflowInstanceId>(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        var ids = page.Items.Select(x => x.Id).ToList();
        return Page.Of(ids, page.TotalCount);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        var record = Map(instance);
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        var record = Map(instance);
        await store.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        var record = Map(instance);
        await store.UpdateAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        var records = Map(instances);
        await store.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, WorkflowInstanceFilter filter)
    {
        query
            .Is(nameof(WorkflowInstance.Id), filter.Id)
            .In(nameof(WorkflowInstance.Id), filter.Ids)
            .Is(nameof(WorkflowInstance.DefinitionId), filter.DefinitionId)
            .In(nameof(WorkflowInstance.DefinitionId), filter.DefinitionIds)
            .Is(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionId)
            .In(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionIds)
            .In(nameof(WorkflowInstance.ParentWorkflowInstanceId), filter.ParentWorkflowInstanceIds)
            .Is(nameof(WorkflowInstance.Status), filter.WorkflowStatus?.ToString())
            .Is(nameof(WorkflowInstance.SubStatus), filter.WorkflowSubStatus?.ToString())
            .Is(nameof(WorkflowInstance.Name), filter.Version)
            .Is(nameof(WorkflowInstance.CorrelationId), filter.CorrelationId)
            .In(nameof(WorkflowInstance.CorrelationId), filter.CorrelationIds)
            .In(nameof(WorkflowInstance.Status), filter.WorkflowStatuses?.Select(x => x.ToString()))
            .In(nameof(WorkflowInstance.SubStatus), filter.WorkflowSubStatuses?.Select(x => x.ToString()))
            .AndWorkflowInstanceSearchTerm(filter.SearchTerm);
    }

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    private Page<WorkflowInstance> Map(Page<WorkflowInstanceRecord> source)
    {
        var items = Map(source.Items).ToList();
        return Page.Of(items, source.TotalCount);
    }

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceRecord> source)
    {
        return source.Select( Map);
    }

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    private IEnumerable<WorkflowInstanceRecord> Map(IEnumerable<WorkflowInstance> source)
    {
        return source.Select(Map);
    }

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    private WorkflowInstance Map(WorkflowInstanceRecord source)
    {
        var workflowState = workflowStateSerializer.Deserialize(source.WorkflowState);
        return new WorkflowInstance
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            IncidentCount = source.IncidentCount,
            IsSystem = source.IsSystem,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            FinishedAt = source.FinishedAt,
            Status = Enum.Parse<WorkflowStatus>(source.Status),
            SubStatus = Enum.Parse<WorkflowSubStatus>(source.SubStatus),
            CorrelationId = source.CorrelationId,
            TenantId = source.TenantId
        };
    }

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    private WorkflowInstanceRecord Map(WorkflowInstance source)
    {
        var workflowState = workflowStateSerializer.Serialize(source.WorkflowState);
        return new WorkflowInstanceRecord
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            IncidentCount = source.IncidentCount,
            IsSystem = source.IsSystem,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            FinishedAt = source.FinishedAt,
            Status = source.Status.ToString(),
            SubStatus = source.SubStatus.ToString(),
            CorrelationId = source.CorrelationId,
            TenantId = source.TenantId
        };
    }
}