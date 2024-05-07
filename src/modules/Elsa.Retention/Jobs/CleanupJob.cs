using Elsa.Retention.Options;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Retention.Jobs;

  /// <summary>
    /// Deletes all workflow instances that are older than a specified threshold (configured through options).
    /// </summary>
    public class CleanupJob
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new cleanup job
        /// </summary>
        /// <param name="workflowInstanceStore"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public CleanupJob(
            IWorkflowInstanceStore workflowInstanceStore,
            IOptions<CleanupOptions> options,
            ILogger<CleanupJob> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Executes the cleanup job
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = DateTimeOffset.Now.Subtract(_options.TimeToLive);
            WorkflowInstanceFilter specification = (WorkflowInstanceFilter)_options.WorkflowInstanceFilter.Clone();

            specification.TimestampFilters ??= new List<TimestampFilter>();
            
            specification.TimestampFilters.Add(
                new TimestampFilter
                {
                    Column = nameof(WorkflowInstance.CreatedAt),
                    Operator = TimestampFilterOperator.LessThanOrEqual,
                    Timestamp = threshold.ToUniversalTime()
                }
            );
            
            long count =  await _workflowInstanceStore.DeleteAsync(specification, cancellationToken);
            _logger.LogInformation("Deleted {WorkflowInstanceCount} workflow instances", count);
        }
    }    