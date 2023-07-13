using Elsa.Workflows.Core.Memory;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="ICollection{Variable}"/>.
/// </summary>
public static class VariableCollectionExtensions
{
    /// <summary>
    /// Adds the specified variable to the collection if it doesn't already exist.
    /// </summary>
    public static void Declare(this ICollection<Variable> variables, Variable variable)
    {
        var existingVariable = variables.FirstOrDefault(x => x.Name == variable.Name);

        if (existingVariable == null)
            variables.Add(variable);
    }
}