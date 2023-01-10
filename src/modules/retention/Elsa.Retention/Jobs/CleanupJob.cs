using System.Collections.Generic;
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
        private readonly IRetentionSpecificationFilter _retentionSpecificationFilter;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        public CleanupJob(
            IWorkflowInstanceStore workflowInstanceStore,
            IClock clock,
            IRetentionSpecificationFilter retentionSpecificationFilter,
            IOptions<CleanupOptions> options,
            ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _retentionSpecificationFilter = retentionSpecificationFilter;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _clock.GetCurrentInstant().Minus(_options.TimeToLive);
            var specification = new WorkflowCreatedBeforeSpecification(threshold).And(_retentionSpecificationFilter.GetSpecification());
            var take = _options.BatchSize;
            var orderBy = new OrderBy<WorkflowInstance>(x => x.CreatedAt, SortDirection.Descending);

            // Collect workflow instances to be deleted.
            while (true)
            {
                var paging = new Paging(0, take); ;

                var workflowInstances = await _workflowInstanceStore
                    .FindManyAsync(specification,(wf)=>wf.Id ,orderBy, paging, cancellationToken)
                    .ToList();

                // Delete collected workflow instances.
                await DeleteManyAsync(workflowInstances, cancellationToken);

                if (workflowInstances.Count < take)
                    break;
            }
        }

        private async Task DeleteManyAsync(ICollection<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting {WorkflowInstanceCount} workflow instances", workflowInstanceIds.Count);
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowInstanceStore.DeleteManyAsync(specification, cancellationToken);
        }
    }    
}