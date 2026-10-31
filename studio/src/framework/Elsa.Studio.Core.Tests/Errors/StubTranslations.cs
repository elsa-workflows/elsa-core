using Elsa.Studio.Localization;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>Translates only the keys it is given; <see cref="DefaultLocalizer"/> falls back to the key for the rest.</summary>
internal sealed class StubTranslations(Dictionary<string, string> translations) : ILocalizationProvider
{
    public string? GetTranslation(string key) => translations.GetValueOrDefault(key);
}
