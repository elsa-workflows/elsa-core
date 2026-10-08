namespace Elsa.Studio.Localization;

/// <summary>
/// Defines the contract for localization provider.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>
    /// Gets the translation for the specified key.
    /// </summary>
    string? GetTranslation(string key);
}
