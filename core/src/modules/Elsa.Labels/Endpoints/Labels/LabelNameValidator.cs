using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Endpoints.Labels;

/// <summary>
/// Validates a label name before it reaches the entity and the store, so invalid names surface as client errors instead of 500s.
/// Uses the same trimming and normalization as <see cref="Label.Name"/>.
/// </summary>
internal static class LabelNameValidator
{
    public const string RequiredMessage = "Label name is required.";
    public static readonly string TooLongMessage = $"Label name cannot exceed {Label.NameMaxLength} characters.";
    public const string DuplicateMessage = "A label with this name already exists.";

    /// <summary>
    /// Returns the error message for a missing or over-long name, or <c>null</c> when the name is valid.
    /// </summary>
    public static string? GetNameError(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RequiredMessage;

        return name.Trim().Length > Label.NameMaxLength ? TooLongMessage : null;
    }

    public static async Task<bool> IsDuplicateAsync(ILabelStore store, string name, string? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = Label.NormalizeName(name);
        var labels = await store.ListAsync(cancellationToken: cancellationToken);
        return labels.Items.Any(x => x.Id != excludeId && x.NormalizedName == normalizedName);
    }
}
