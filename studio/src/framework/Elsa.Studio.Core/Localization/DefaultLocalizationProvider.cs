namespace Elsa.Studio.Localization;

internal class DefaultLocalizationProvider : ILocalizationProvider
{
    /// <summary>
    /// Provides the get translation.
    /// </summary>
    public string? GetTranslation(string key)
    {
        return key;
    }
}
