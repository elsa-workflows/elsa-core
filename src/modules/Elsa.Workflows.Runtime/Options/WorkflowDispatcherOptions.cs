namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Provides options to the workflow dispatcher.
/// </summary>
public class WorkflowDispatcherOptions
{
    /// <summary>
    /// A list of available channels to dispatch workflows to.
    /// </summary>
    /// <remarks>
    /// Channels are used to dispatch workflows to different queues or endpoints.
    /// </remarks>
    public ICollection<DispatcherChannel> Channels { get; set; } = new List<DispatcherChannel>();
}