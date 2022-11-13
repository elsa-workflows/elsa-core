using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IBehavior : ISignalHandler
{
    /// <summary>
    /// The owner of this behavior.
    /// </summary>
    IActivity Owner { get; }
    
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}