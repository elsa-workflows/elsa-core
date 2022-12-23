using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Represents a container for <see cref="Variable"/>s.
/// </summary>
public interface IVariableContainer
{
    ICollection<Variable> Variables { get; }
}