using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Endpoints.Labels;

/// <summary>
/// Validates a label name before it reaches the entity and the store, so invalid names surface as client errors instead of 500s.
/// </summary>
internal static class LabelNameValidator
{
    public const string TooLongMessage = "Label name cannot exceed 255 characters.";
    public const string DuplicateMessage = "A label with this name already exists.";

    public static bool IsTooLong(string name) => name.Trim().Length > Label.NameMaxLength;

    public static async Task<bool> IsDuplicateAsync(ILabelStore store, string name, string? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var labels = await store.ListAsync(cancellationToken: cancellationToken);
        return labels.Items.Any(x => x.Id != excludeId && x.NormalizedName == normalizedName);
    }
}
