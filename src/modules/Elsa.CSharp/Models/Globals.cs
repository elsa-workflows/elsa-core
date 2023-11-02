namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to global objects, such as the workflow execution context.
/// </summary>
public class Globals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Globals"/> class.
    /// </summary>
    public Globals(ExecutionContextProxy executionContext)
    {
        ExecutionContext = executionContext;
    }

    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    public ExecutionContextProxy ExecutionContext { get; }
}