using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Options;

/// <summary>
/// Options for the mediator.
/// </summary>
public class MediatorOptions
{
    /// <summary>
    /// The max number of workers to process commands.
    /// </summary>
    public int CommandWorkerCount { get; set; } = 4;
    
    /// <summary>
    /// The max number of workers to process notifications.
    /// </summary>
    public int NotificationWorkerCount { get; set; } = 4;

    /// <summary>
    /// The default publishing strategy to use.
    /// </summary>
    public IEventPublishingStrategy DefaultPublishingStrategy { get; set; } = NotificationStrategy.Sequential;

    /// <summary>
    /// The default command strategy to use.
    /// </summary>
    public ICommandStrategy DefaultCommandStrategy { get; set; } = CommandStrategy.Default;
}