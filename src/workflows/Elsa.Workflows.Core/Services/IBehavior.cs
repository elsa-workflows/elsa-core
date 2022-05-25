using Elsa.Models;

namespace Elsa.Services;

public interface IBehavior : ISignalHandler
{
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}