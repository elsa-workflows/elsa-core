using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization;

/// <summary>
/// Interface for localization.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Gets the localized string for the given key.
    /// </summary>
    /// <param name="key">The key for the localized string.</param>
    LocalizedString this[string? key] { get; }

    /// <summary>
    /// Gets the localized string for the given key and formats it with the provided arguments.
    /// </summary>
    /// <param name="key">The key for the localized string.</param>
    /// <param name="arguments">The arguments to format the localized string with.</param>
    LocalizedString this[string? key, params object[] arguments] { get; }
}
