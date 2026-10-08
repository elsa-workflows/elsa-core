using System.Globalization;
using Elsa.Studio.Localization;

namespace Elsa.Studio.Translations;

internal class ResxLocalizationProvider : ILocalizationProvider
{
    /// <summary>
    /// Gets the translation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The result of the operation.</returns>
    public string GetTranslation(string key)
    {
        return Elsa.Studio.Translations.Translations.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
