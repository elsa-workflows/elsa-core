using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Extensions;

public static class NodeExtensions
{
    public static IEnumerable<Input> GetInputs(this INode node)
    {
        var inputProps = node.GetType().GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();

        var query =
            from inputProp in inputProps
            select (Input?)inputProp.GetValue(node)
            into input
            where input != null
            select input;

        return query.Select(x => x!).ToList();
    }
        
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