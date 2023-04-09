using System.Threading.Channels;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using a local background worker.
/// </summary>
public class LocalBackgroundActivityScheduler : IBackgroundActivityScheduler
{
    private readonly Channel<ScheduledBackgroundActivity> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalBackgroundActivityScheduler"/> class.
    /// </summary>
    /// <param name="channel">The channel to write to.</param>
    public LocalBackgroundActivityScheduler(Channel<ScheduledBackgroundActivity> channel)
    {
        _channel = channel;
    }
    
    /// <inheritdoc />
    public async Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(scheduledBackgroundActivity, cancellationToken);
        return "";
    }
}