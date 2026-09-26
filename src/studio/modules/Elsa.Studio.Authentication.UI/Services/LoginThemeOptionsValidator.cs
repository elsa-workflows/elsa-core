using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Services;

/// <summary>
/// Validates login theme registrations and the configured selection during startup.
/// </summary>
public sealed class LoginThemeOptionsValidator : IValidateOptions<LoginThemeOptions>
{
    private readonly IEnumerable<LoginThemeRegistration> _registrations;
    private readonly IOptions<StudioThemeOptions> _studioThemeOptions;

    public LoginThemeOptionsValidator(IEnumerable<LoginThemeRegistration> registrations)
        : this(registrations, Microsoft.Extensions.Options.Options.Create(
            new StudioThemeOptions { Theme = Elsa.Studio.Models.StudioThemeIds.Classic }))
    {
    }

    public LoginThemeOptionsValidator(
        IEnumerable<LoginThemeRegistration> registrations,
        IOptions<StudioThemeOptions> studioThemeOptions)
    {
        _registrations = registrations;
        _studioThemeOptions = studioThemeOptions;
    }

    public ValidateOptionsResult Validate(string? name, LoginThemeOptions options)
    {
        var result = LoginThemeRegistrationRules.ValidateAndSelect(
            _registrations,
            options.Theme,
            _studioThemeOptions.Value.Theme);

        return result.Errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors);
    }
}
