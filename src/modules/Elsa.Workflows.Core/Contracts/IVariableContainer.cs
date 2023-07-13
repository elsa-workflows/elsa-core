using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Contracts;

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