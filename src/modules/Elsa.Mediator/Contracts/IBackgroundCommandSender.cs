namespace Elsa.Mediator.Contracts;

/// <summary>
/// Send requests to be processed asynchronously in the background. 
/// </summary>
public interface IBackgroundCommandSender
{
    /// <summary>
    /// Submits the specified command to a channel writer. The channel is processed asynchronously from a background service.
    /// </summary>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}