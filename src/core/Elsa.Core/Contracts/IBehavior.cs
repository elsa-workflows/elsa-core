using Elsa.Models;

namespace Elsa.Contracts;

public interface IBehavior : ISignalHandler
{
    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    ValueTask ExecuteAsync(ActivityExecutionContext context);
}