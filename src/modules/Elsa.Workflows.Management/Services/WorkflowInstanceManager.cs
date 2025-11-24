using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceManager(
    IWorkflowInstanceStore store,
    IWorkflowInstanceFactory workflowInstanceFactory,
    INotificationSender notificationSender,
    WorkflowStateMapper workflowStateMapper,
    IWorkflowStateExtractor workflowStateExtractor,
    IWorkflowStateSerializer workflowStateSerializer,
    IOptions<ManagementOptions> managementOptions,
    ILogger<WorkflowInstanceManager> logger)
    : IWorkflowInstanceManager
{
    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindByIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = instanceId
        };
        var count = await store.CountAsync(filter, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(workflowInstance, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> SaveAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await SaveAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> SaveAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var workflowState = ExtractWorkflowState(workflowExecutionContext);
        return await SaveAsync(workflowState, cancellationToken);
    }

    public async Task CreateAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(workflowInstance, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
    }

    public async Task<WorkflowInstance> CreateAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await CreateAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    public async Task<WorkflowInstance> CreateAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var workflowState = ExtractWorkflowState(workflowExecutionContext);
        return await CreateAsync(workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(workflowInstance, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
    }

    public async Task<WorkflowInstance> UpdateAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await UpdateAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var instance = await store.FindAsync(filter, cancellationToken);

        if (instance == null)
            return false;

        var ids = new[]
        {
            instance.Id
        };
        await notificationSender.SendAsync(new WorkflowInstancesDeleting(ids), cancellationToken);
        await store.DeleteAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstancesDeleted(ids), cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var batchSize = managementOptions.Value.BulkDeleteBatchSize;
        var totalDeleted = 0L;
        var allDeletedIds = new List<string>();
        
        logger.LogInformation("Starting bulk delete operation with batch size {BatchSize}", batchSize);

        while (true)
        {
            // Get IDs of records to delete in this batch
            var batchIds = await store.FindManyIdsAsync(filter, new PageArgs { Offset = 0, Limit = batchSize }, cancellationToken);
            var batchIdsList = batchIds.Items.ToList();
            
            if (batchIdsList.Count == 0)
                break;

            logger.LogDebug("Deleting batch of {Count} workflow instances", batchIdsList.Count);

            // Send notification before deleting this batch
            await notificationSender.SendAsync(new WorkflowInstancesDeleting(batchIdsList), cancellationToken);

            // Delete this batch
            var batchFilter = new WorkflowInstanceFilter { Ids = batchIdsList };
            var deletedCount = await store.DeleteAsync(batchFilter, cancellationToken);
            
            totalDeleted += deletedCount;
            allDeletedIds.AddRange(batchIdsList);

            // Send notification after deleting this batch
            await notificationSender.SendAsync(new WorkflowInstancesDeleted(batchIdsList), cancellationToken);

            logger.LogDebug("Deleted {DeletedCount} workflow instances in batch, total deleted: {TotalDeleted}", deletedCount, totalDeleted);

            // If we deleted fewer items than the batch size, we're done
            if (batchIdsList.Count < batchSize)
                break;
        }

        logger.LogInformation("Bulk delete operation completed. Total deleted: {TotalDeleted}", totalDeleted);
        return totalDeleted;
    }

    /// <inheritdoc />
    public WorkflowState ExtractWorkflowState(WorkflowExecutionContext workflowExecutionContext)
    {
        return workflowStateExtractor.Extract(workflowExecutionContext);
    }

    /// <inheritdoc />
    public string SerializeWorkflowState(WorkflowState workflowState)
    {
        return workflowStateSerializer.Serialize(workflowState);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAndCommitWorkflowInstanceAsync(Workflow workflow, WorkflowInstanceOptions? options = null, CancellationToken cancellationToken = default)
    {
        var workflowInstance = CreateWorkflowInstance(workflow, options);
        await SaveAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    /// <inheritdoc />
    public WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowInstanceOptions? options = null)
    {
        return workflowInstanceFactory.CreateWorkflowInstance(workflow, options);
    }
}