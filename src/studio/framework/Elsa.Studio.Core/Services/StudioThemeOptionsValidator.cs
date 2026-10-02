using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Services;

/// <summary>
/// Validates theme registrations and the configured selection during startup.
/// </summary>
public sealed class StudioThemeOptionsValidator(
    IEnumerable<StudioThemeRegistration> registrations) : IValidateOptions<StudioThemeOptions>
{
    public ValidateOptionsResult Validate(string? name, StudioThemeOptions options)
    {
        var result = StudioThemeRegistrationRules.ValidateAndSelect(registrations, options.Theme);

        return result.Errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors);
    }
}
