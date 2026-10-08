using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

internal static class StudioThemeRegistrationRules
{
    public static StudioThemeSelection ValidateAndSelect(
        IEnumerable<StudioThemeRegistration> registrations,
        string? configuredTheme)
    {
        var materialized = registrations.ToArray();
        var errors = new List<string>();

        foreach (var registration in materialized)
        {
            if (string.IsNullOrWhiteSpace(registration.Id))
                errors.Add("Studio theme registrations require a non-blank ID.");

            if (!typeof(IThemeProvider).IsAssignableFrom(registration.ProviderType))
                errors.Add($"Studio theme '{registration.Id}' must use an {nameof(IThemeProvider)} provider type.");
        }

        foreach (var duplicate in materialized
                     .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                     .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
        {
            errors.Add($"Studio theme '{duplicate.First().Id}' is registered more than once.");
        }

        StudioThemeRegistration? selected = null;
        if (string.IsNullOrWhiteSpace(configuredTheme))
        {
            errors.Add("Presentation:Theme must not be blank.");
        }
        else
        {
            selected = materialized.FirstOrDefault(
                x => string.Equals(x.Id, configuredTheme, StringComparison.OrdinalIgnoreCase));

            if (selected is null)
                errors.Add($"Presentation:Theme selects '{configuredTheme}', but no matching Studio theme is registered.");
        }

        return new(selected, errors);
    }
}

internal sealed record StudioThemeSelection(
    StudioThemeRegistration? Selected,
    IReadOnlyList<string> Errors);
