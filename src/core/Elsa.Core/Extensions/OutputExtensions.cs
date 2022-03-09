using Elsa.Models;

namespace Elsa.Extensions;

public static class OutputExtensions
{
    /// <summary>
    /// Creates an input that references the specified output's value.
    /// </summary>
    public static Input<T> CreateInput<T>(this Output<T>? output) => new(output);
}