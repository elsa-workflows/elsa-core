using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Retention.Contracts;
using Elsa.Retention.Models;
using Elsa.Retention.Options;
using Elsa.Retention.Stores;
using MediatR;
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
        private readonly IRetentionWorkflowInstanceStore _workflowInstanceStore;
        private readonly IClock _clock;
        private readonly IRetentionFilterPipeline _retentionFilterPipeline;
        private readonly IRetentionSpecificationFilter _specificationFilter;
        private readonly IMediator _mediator;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        public CleanupJob(
            IRetentionWorkflowInstanceStore workflowInstanceStore,
            IClock clock,
            IRetentionFilterPipeline retentionFilterPipeline,
            IRetentionSpecificationFilter specificationFilter,
            IMediator mediator,
            IOptions<CleanupOptions> options,
            ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _retentionFilterPipeline = retentionFilterPipeline;
            _specificationFilter = specificationFilter;
            _mediator = mediator;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _clock.GetCurrentInstant().Minus(_options.TimeToLive);
            ISpecification<WorkflowInstance> specification = new WorkflowCreatedBeforeSpecification(threshold);
            var take = _options.BatchSize;
            var orderBy = new OrderBy<WorkflowInstance>(x => x.CreatedAt, SortDirection.Ascending);
            
            // Collect workflow instances to be deleted.
            while (true)
            {
                var paging = new Paging(0, take);

                specification = specification.And(_specificationFilter.GetSpecification());

                var workflowInstanceId = await _workflowInstanceStore.FindManyWithDTOAsync
                    (specification, orderBy, paging, (wfi) => new RetentionWorkflowId { Id = wfi.Id }, cancellationToken);

                // Delete collected workflow instances.
                await DeleteManyAsync(workflowInstanceId.Select(x=>x.Id).ToArray(), cancellationToken);

                if (workflowInstanceId.Count() < take)
                    break;
            }           
        }

        private async Task DeleteManyAsync(ICollection<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting {WorkflowInstanceCount} workflow instances", workflowInstanceIds.Count);
            await (_workflowInstanceStore as EntityFrameworkWorkflowInstanceStore).DeleteManyByIdsAsync(workflowInstanceIds, cancellationToken);

            //WorkflowInstanceDeleted and ManyWorkflowInstancesDeleted events only use Id.
            var instances = workflowInstanceIds.Select(id => new WorkflowInstance() { Id = id });
            foreach (var instance in instances)
                await _mediator.Publish(new WorkflowInstanceDeleted(instance), cancellationToken);

            await _mediator.Publish(new ManyWorkflowInstancesDeleted(instances), cancellationToken);
        }
    }
}