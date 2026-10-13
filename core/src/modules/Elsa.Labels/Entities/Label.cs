using Elsa.Common.Entities;

namespace Elsa.Labels.Entities;

/// <summary>
/// Represents an individual label.
/// </summary>
public class Label : Entity
{
    /// <summary>
    /// Shared maximum for <see cref="Name"/> and <see cref="NormalizedName"/>.
    /// Matches the MySQL unique-index limit that every provider's schema must honor.
    /// </summary>
    public const int NameMaxLength = 255;

    private string _name = default!;

    public string Name
    {
        get => _name;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Name));
            value = value.Trim();

            if (value.Length > NameMaxLength)
            {
                throw new ArgumentException(
                    $"Label name cannot exceed {NameMaxLength} characters.",
                    nameof(Name));
            }

            _name = value;
            NormalizedName = NormalizeName(value);
        }
    }

    public string NormalizedName { get; set; } = default!;

    /// <summary>
    /// Normalizes a label name the way <see cref="Name"/> does: trimmed and lower-cased.
    /// </summary>
    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
    public string? Description { get; set; }
    public string? Color { get; set; }
}