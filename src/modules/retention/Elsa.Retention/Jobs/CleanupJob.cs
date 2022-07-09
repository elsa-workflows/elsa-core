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
        private readonly IClock _clock;
        private readonly IRetentionFilterPipeline _retentionFilterPipeline;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        public CleanupJob(
            IWorkflowInstanceStore workflowInstanceStore,
            IClock clock,
            IRetentionFilterPipeline retentionFilterPipeline,
            IOptions<CleanupOptions> options,
            ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _retentionFilterPipeline = retentionFilterPipeline;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _clock.GetCurrentInstant().Minus(_options.TimeToLive);
            var specification = new WorkflowCreatedBeforeSpecification(threshold);
            var currentPage = 0;
            var take = _options.BatchSize;
            var orderBy = new OrderBy<WorkflowInstance>(x => x.CreatedAt, SortDirection.Descending);
            var collectedWorkflowInstanceIds = new List<string>();

            // Collect workflow instances to be deleted.
            while (true)
            {
                var paging = Paging.Page(currentPage++, take);

                var workflowInstances = await _workflowInstanceStore
                    .FindManyAsync(specification, orderBy, paging, cancellationToken)
                    .ToList();

                var filteredWorkflowInstances = await _retentionFilterPipeline.FilterAsync(workflowInstances, cancellationToken).ToList();
                collectedWorkflowInstanceIds.AddRange(filteredWorkflowInstances.Select(x => x.Id));

                if (workflowInstances.Count < take)
                    break;
            }
            
            // Delete collected workflow instances.
            await DeleteManyAsync(collectedWorkflowInstanceIds, cancellationToken);
        }

        private async Task DeleteManyAsync(ICollection<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting {WorkflowInstanceCount} workflow instances", workflowInstanceIds.Count);
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowInstanceStore.DeleteManyAsync(specification, cancellationToken);
        }
    }
}