namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to global objects, such as the workflow execution context.
/// </summary>
public class Globals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Globals"/> class.
    /// </summary>
    public Globals(ExecutionContextProxy context)
    {
        Context = context;
    }

    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    public ExecutionContextProxy Context { get; }
}