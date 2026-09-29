using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Resolvers;

/// <summary>
/// A default resolver.
/// </summary>
public class DefaultActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => -1;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => true;

    /// <inheritdoc />
    public ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(JsonObject activity, CancellationToken cancellationToken = default)
    {
        var containedActivities =
            from prop in activity
            where prop.Value is JsonObject jsonObject && jsonObject.IsActivity() || prop.Value is JsonArray
            let isCollection = prop.Value is JsonArray
            let containedItems = isCollection ? ((JsonArray)prop.Value).ToArray() : new[] { prop.Value.AsObject() }
            from containedItem in containedItems where containedItem is JsonObject && containedItem.AsObject().IsActivity()
            let containedObject = containedItem.AsObject()
            select new EmbeddedActivity(containedObject, prop.Key);

        return new(containedActivities.ToList());
    }
}