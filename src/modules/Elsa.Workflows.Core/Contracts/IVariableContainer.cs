using Elsa.Workflows.Memory;

namespace Elsa.Workflows;

/// <summary>
/// Represents a container for <see cref="Variable"/>s.
/// </summary>
public interface IVariableContainer : IActivity
{
    /// <summary>
    /// A collection of variables within the scope of the variable container.
    /// </summary>
    ICollection<Variable> Variables { get; }
}