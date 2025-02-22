using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

public class WorkflowHeartbeatMiddleware(WorkflowMiddlewareDelegate next, IOptions<RuntimeOptions> options, ILogger<WorkflowHeartbeatMiddleware> logger, ILoggerFactory loggerFactory) : WorkflowExecutionMiddleware(next)
{
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var livenessThreshold = options.Value.WorkflowLivenessThreshold;
        var heartbeatInterval = TimeSpan.FromTicks((long)(livenessThreshold.Ticks * 0.6));
        logger.LogDebug("Workflow heartbeat interval: {Interval}", heartbeatInterval);
        using var heartbeat = new WorkflowHeartbeat(async () => await UpdateTimestampAsync(context), heartbeatInterval, loggerFactory);
        await Next(context);
    }

    private async Task UpdateTimestampAsync(WorkflowExecutionContext context)
    {
        var workflowInstanceStore = context.GetRequiredService<IWorkflowInstanceStore>();
        var clock = context.GetRequiredService<ISystemClock>();
        var workflowInstanceId = context.Id;
        await workflowInstanceStore.UpdateUpdatedTimestampAsync(workflowInstanceId, clock.UtcNow, context.CancellationToken);
    }

    private class WorkflowHeartbeat : IDisposable
    {
        private readonly Timer _timer;
        private readonly Func<Task> _pulseAction;
        private readonly TimeSpan _interval;
        private readonly ILogger<WorkflowHeartbeat> _logger;

        public WorkflowHeartbeat(Func<Task> pulseAction, TimeSpan interval, ILoggerFactory loggerFactory)
        {
            _pulseAction = pulseAction;
            _interval = interval;
            _logger = loggerFactory.CreateLogger<WorkflowHeartbeat>();
            _timer = new(GeneratePulseAsync, null, _interval, Timeout.InfiniteTimeSpan);
        }

        private async void GeneratePulseAsync(object? state)
        {
            try
            {
                _logger.LogDebug("Generating pulse");
                await _pulseAction();
                _timer.Change(_interval, Timeout.InfiniteTimeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating a workflow heartbeat.");
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}