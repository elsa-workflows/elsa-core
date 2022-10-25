namespace Elsa.Workflows.Core.Models;

public enum ActivityKind
{
    /// <summary>
    /// Always run synchronously.
    /// </summary>
    Action,
    
    /// <summary>
    /// Can be used to trigger new workflows.
    /// </summary>
    Trigger,
    
    /// <summary>
    /// Always run asynchronously.
    /// </summary>
    Job,
    
    /// <summary>
    /// Run synchronously by default (like <see cref="Action"/>, but can be configured to run asynchronously (like <see cref="Job"/>).
    /// </summary>
    Task
}