using Elsa.Studio.Settings.Contracts;
using Elsa.Studio.Settings.Models;

namespace Elsa.Studio.Settings.Services;

public sealed class SettingsSectionRegistry(IEnumerable<ISettingsSectionProvider> providers) : ISettingsSectionRegistry
{
    public async ValueTask<IReadOnlyList<SettingsSectionDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sections = new List<SettingsSectionDescriptor>();
        foreach (var provider in providers)
            sections.AddRange(await provider.GetSectionsAsync(cancellationToken));

        Validate(sections);
        return sections
            .OrderBy(x => x.Order)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Validate(IEnumerable<SettingsSectionDescriptor> sections)
    {
        var materialized = sections.ToArray();
        if (materialized.Any(x =>
                string.IsNullOrWhiteSpace(x.Id) ||
                string.IsNullOrWhiteSpace(x.DisplayName) ||
                string.IsNullOrWhiteSpace(x.Href)))
            throw new InvalidOperationException("Settings sections require an ID, display name, and destination.");

        var duplicate = materialized
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Settings section '{duplicate.Key}' is registered more than once.");
    }
}
