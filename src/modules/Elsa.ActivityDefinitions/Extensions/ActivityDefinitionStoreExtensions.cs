using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Models;

namespace Elsa.ActivityDefinitions.Extensions;

public static class ActivityDefinitionStoreExtensions
{
    public static async Task<IEnumerable<ActivityDefinition>> ListAsync(this IActivityDefinitionStore store, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var page = await store.ListAsync(versionOptions, cancellationToken: cancellationToken);
        return page.Items;
    }
}