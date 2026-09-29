using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization;

/// <inheritdoc />
public class DefaultLocalizer(ILocalizationProvider provider) : ILocalizer
{
    /// <inheritdoc />
    public LocalizedString this[string? key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
                return new LocalizedString(string.Empty, string.Empty, true);

            var notFound = false;
            var translation = provider.GetTranslation(key);

            if (string.IsNullOrEmpty(translation))
            {
                translation = key;
                notFound = true;
            }

            return new LocalizedString(key, translation, notFound);
        }
    }

    /// <summary>
    /// Gets the localized string with the specified key and formats it with the provided arguments.
    /// </summary>
    /// <param name="key">The key of the localized string.</param>
    /// <param name="arguments">The arguments to format the localized string with.</param>
    /// <returns>The localized string formatted with the provided arguments.</returns>
    public LocalizedString this[string? key, params object[] arguments]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
                return new LocalizedString(string.Empty, string.Empty, true);

            var notFound = false;
            var translation = provider.GetTranslation(key);

            if (string.IsNullOrEmpty(translation))
            {
                translation = string.Format(key, arguments);
                notFound = true;
            }
            else
            {
                translation = string.Format(translation, arguments);
            }

            return new LocalizedString(key, translation, notFound);
        }
    }
}
