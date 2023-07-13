using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides a common interface to access the current execution context.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// The unique ID of this execution context.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// The expression execution context.
    /// </summary>
    ExpressionExecutionContext ExpressionExecutionContext { get; }
    
    /// <summary>
    /// Returns variables declared the current execution context.
    /// </summary>
    IEnumerable<Variable> Variables { get; }

    /// <summary>
    /// A dictionary of values that can be associated with this activity execution context.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; }
}