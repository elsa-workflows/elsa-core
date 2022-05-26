using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class OutputExtensions
{
    /// <summary>
    /// Creates an input that references the specified output's value.
    /// </summary>
    public static Input<T> CreateInput<T>(this Output output) => new(output);
}