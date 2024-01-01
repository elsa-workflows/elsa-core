using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ActivityNode"/>.
/// </summary>
public static class ActivityNodeExtensions
{
    /// <summary>
    /// Returns a flattened list of the specified node and all its descendants.
    /// </summary>
    public static IEnumerable<ActivityNode> Flatten(this ActivityNode root)
    {
        yield return root;

        foreach (var node in root.Children)
        {
            var children = node.Flatten();

            foreach (var child in children)
                yield return child;
        }
    }
}