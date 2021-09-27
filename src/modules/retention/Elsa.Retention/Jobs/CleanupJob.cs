using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Retention.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Elsa.Retention.Jobs
{
    public class CleanupJob
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IClock _clock;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        public CleanupJob(IWorkflowInstanceStore workflowInstanceStore, IClock clock, IOptions<CleanupOptions> options, ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _clock.GetCurrentInstant().Minus(_options.TimeToLive);
            var specification = new WorkflowCreatedBeforeSpecification(threshold).And(new WorkflowFinishedStatusSpecification());
            var take = _options.PageSize;
            IList<string> workflowInstanceIds;

            do
            {
                workflowInstanceIds = (await _workflowInstanceStore.FindManyAsync(specification, new OrderBy<WorkflowInstance>(x => x.CreatedAt, SortDirection.Descending), new Paging(0, take), cancellationToken: cancellationToken))
                    .Select(x => x.Id).ToList();
                _logger.LogInformation("Deleting {WorkflowInstanceCount} workflow instances", workflowInstanceIds.Count);

                if (workflowInstanceIds.Any())
                    await DeleteManyAsync(workflowInstanceIds, cancellationToken);
            } while (workflowInstanceIds.Any());
        }

        private async Task DeleteManyAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
        {
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowInstanceStore.DeleteManyAsync(specification, cancellationToken);
        }
    }
}