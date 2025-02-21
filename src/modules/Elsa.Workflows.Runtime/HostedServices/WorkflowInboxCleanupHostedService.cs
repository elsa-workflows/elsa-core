using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Cleans up expired messages from the database.
/// </summary>
public class WorkflowInboxCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkflowInboxCleanupOptions> _options;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public WorkflowInboxCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkflowInboxCleanupOptions> options,
        ILogger<WorkflowInboxCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.IsEnabled)
        {
            _logger.LogInformation("Expired workflow inbox messages cleanup service is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Entering expired workflow inbox messages cleanup service loop");

            try
            {
                await CleanupExpiredMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired workflow inbox messages");
            }

            var sweepInterval = _options.Value.SweepInterval;
            _logger.LogInformation("Expired messages cleanup service is going to sleep for {SweepInterval} minutes", sweepInterval.TotalMinutes);
            await Task.Delay(sweepInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var workflowInboxMessageStore = scope.ServiceProvider.GetRequiredService<IWorkflowInboxMessageStore>();
        var filter = new WorkflowInboxMessageFilter
        {
            IsExpired = true
        };

        var pageArgs = PageArgs.FromRange(0, _options.Value.BatchSize);
        var deleteCount = await workflowInboxMessageStore.DeleteManyAsync(filter, pageArgs, cancellationToken);
        _logger.LogInformation("Cleaned up {DeleteCount} expired messages", deleteCount);
    }
}