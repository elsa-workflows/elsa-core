using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class NodeExtensions
{
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