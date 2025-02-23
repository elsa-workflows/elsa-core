using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class HeartbeatGenerator : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<Task> _heartbeatAction;
    private readonly TimeSpan _interval;
    private readonly ILogger<HeartbeatGenerator> _logger;

    public HeartbeatGenerator(Func<Task> heartbeatAction, TimeSpan interval, ILoggerFactory loggerFactory)
    {
        _heartbeatAction = heartbeatAction;
        _interval = interval;
        _logger = loggerFactory.CreateLogger<HeartbeatGenerator>();
        _timer = new(GenerateHeartbeatAsync, null, _interval, Timeout.InfiniteTimeSpan);
    }

    private async void GenerateHeartbeatAsync(object? state)
    {
        try
        {
            _logger.LogDebug("Generating heartbeat");
            await _heartbeatAction();
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