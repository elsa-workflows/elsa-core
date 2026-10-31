using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.AI.Host.Services;

public class ActivityGroundingSearchService(ActivityGroundingMapper mapper)
{
    public IReadOnlyCollection<ActivityDescriptor> Search(
        IActivityRegistry registry,
        string? query,
        string? category,
        string? type,
        int? version,
        string? input,
        string? output,
        bool? trigger)
    {
        var descriptors = registry.ListAll().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
            descriptors = descriptors.Where(x => MatchesQuery(x, query));

        if (!string.IsNullOrWhiteSpace(category))
            descriptors = descriptors.Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(type))
            descriptors = descriptors.Where(x => string.Equals(x.TypeName, type, StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(x.Name, type, StringComparison.OrdinalIgnoreCase));

        if (version != null)
            descriptors = descriptors.Where(x => x.Version == version);

        if (!string.IsNullOrWhiteSpace(input))
            descriptors = descriptors.Where(x => x.Inputs.Any(inputDescriptor => MatchesText(inputDescriptor.Name, input) || MatchesText(inputDescriptor.DisplayName, input)));

        if (!string.IsNullOrWhiteSpace(output))
            descriptors = descriptors.Where(x => x.Outputs.Any(outputDescriptor => MatchesText(outputDescriptor.Name, output) || MatchesText(outputDescriptor.DisplayName, output)));

        if (trigger != null)
            descriptors = descriptors.Where(x => x.IsStart == trigger);

        return descriptors
            .OrderBy(x => x.Category)
            .ThenBy(x => x.DisplayName ?? x.Name)
            .ThenByDescending(x => x.Version)
            .ToList();
    }

    public JsonObject Map(ActivityDescriptor descriptor) =>
        AIGroundingJson.ToJsonObject(mapper.Map(descriptor));

    private static bool MatchesQuery(ActivityDescriptor descriptor, string query) =>
        MatchesText(descriptor.Name, query) ||
        MatchesText(descriptor.DisplayName, query) ||
        MatchesText(descriptor.Description, query) ||
        MatchesText(descriptor.Category, query) ||
        MatchesText(descriptor.Namespace, query) ||
        MatchesText(descriptor.TypeName, query) ||
        descriptor.Inputs.Any(x => MatchesText(x.Name, query) || MatchesText(x.DisplayName, query) || MatchesText(x.Description, query)) ||
        descriptor.Outputs.Any(x => MatchesText(x.Name, query) || MatchesText(x.DisplayName, query) || MatchesText(x.Description, query));

    private static bool MatchesText(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
