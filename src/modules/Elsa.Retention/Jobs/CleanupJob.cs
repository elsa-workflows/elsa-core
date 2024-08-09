using Elsa.Common.Contracts;
using Elsa.Retention.Extensions;
using Elsa.Retention.Options;
using Elsa.Workflows.Management;
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
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly CleanupOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _systemClock;

        /// <summary>
        /// Creates a new cleanup job
        /// </summary>
        /// <param name="workflowInstanceManager"></param>
        /// <param name="options"></param>
        /// <param name="systemClock"></param>
        /// <param name="logger"></param>
        public CleanupJob(
            IWorkflowInstanceManager workflowInstanceManager,
            IOptions<CleanupOptions> options,
            ISystemClock systemClock,
            ILogger<CleanupJob> logger)
        {
            _systemClock = systemClock;
            _options = options.Value;
            _logger = logger;
            _workflowInstanceManager = workflowInstanceManager;
        }

        /// <summary>
        /// Executes the cleanup job
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var threshold = _systemClock.UtcNow.Subtract(_options.TimeToLive);
            WorkflowInstanceFilter specification = _options.WorkflowInstanceFilter.Clone();

            specification.TimestampFilters ??= new List<TimestampFilter>();
            
            specification.TimestampFilters.Add(
                new TimestampFilter
                {
                    Column = nameof(WorkflowInstance.CreatedAt),
                    Operator = TimestampFilterOperator.LessThanOrEqual,
                    Timestamp = threshold
                }
            );

            long count = await _workflowInstanceManager.BulkDeleteAsync(specification, cancellationToken);
            _logger.LogInformation("Deleted {WorkflowInstanceCount} workflow instances", count);
        }
    }    