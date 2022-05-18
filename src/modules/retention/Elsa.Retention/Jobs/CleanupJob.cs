using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Retention.Contracts;
using Elsa.Retention.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Open.Linq.AsyncExtensions;

namespace Elsa.Retention.Jobs
{
    /// <summary>
    /// Deletes all workflow instances that are older than a specified threshold (configured through options).
    /// </summary>
    public class CleanupJob
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
        private readonly IClock _clock;
        private readonly IRetentionFilterPipeline _retentionFilterPipeline;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        public CleanupJob(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowExecutionLogStore workflowExecutionLogStore,
            IClock clock,
            IRetentionFilterPipeline retentionFilterPipeline,
            IOptions<CleanupOptions> options,
            ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _workflowExecutionLogStore = workflowExecutionLogStore;
            _clock = clock;
            _retentionFilterPipeline = retentionFilterPipeline;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _clock.GetCurrentInstant().Minus(_options.TimeToLive);
            var specification = new WorkflowCreatedBeforeSpecification(threshold);
            var take = _options.BatchSize;
            var orderBy = new OrderBy<WorkflowInstance>(x => x.CreatedAt, SortDirection.Descending);

            while (true)
            {
                var paging = new Paging(0, take);

                var workflowInstances = await _workflowInstanceStore
                    .FindManyAsync(specification, orderBy, paging, cancellationToken)
                    .ToList();

                await FilterAndDeleteWorkflowsAsync(workflowInstances, cancellationToken);

                if (workflowInstances.Count < take)
                    break;
            }
        }

        private async Task FilterAndDeleteWorkflowsAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken)
        {
            var filteredWorkflowInstances = await _retentionFilterPipeline.FilterAsync(workflowInstances, cancellationToken).ToList();

            _logger.LogInformation("Deleting {WorkflowInstanceCount} workflow instances", filteredWorkflowInstances.Count);

            if (filteredWorkflowInstances.Any())
            {
                var filteredWorkflowInstancesIds = filteredWorkflowInstances.Select(x => x.Id);
                await DeleteManyAsync(filteredWorkflowInstancesIds, cancellationToken);
                await DeleteManyExecutionLogStoreAsync(filteredWorkflowInstancesIds, cancellationToken);
            }
        }

        private async Task DeleteManyAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowInstanceStore.DeleteManyAsync(specification, cancellationToken);
        }

        private async Task DeleteManyExecutionLogStoreAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            var specification = new Persistence.Specifications.WorkflowExecutionLogRecords.WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowExecutionLogStore.DeleteManyAsync(specification, cancellationToken);
        }
    }
}