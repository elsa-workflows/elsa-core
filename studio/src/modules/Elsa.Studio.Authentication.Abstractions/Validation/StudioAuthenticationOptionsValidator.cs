using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.Abstractions.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.Abstractions.Validation;

/// <summary>
/// Rejects ambiguous or incomplete authentication registrations.
/// </summary>
public sealed class StudioAuthenticationOptionsValidator(
    IEnumerable<StudioAuthenticationProviderRegistration> registrations) : IValidateOptions<StudioAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, StudioAuthenticationOptions options)
    {
        var activeRegistrations = registrations
            .Select(x => x.Provider)
            .Distinct()
            .ToArray();

        if (activeRegistrations.Length > 1)
        {
            return ValidateOptionsResult.Fail(
                $"Conflicting Elsa Studio authentication modes are registered: {string.Join(", ", activeRegistrations)}. " +
                "Register exactly one of Elsa Identity, deprecated Elsa.Studio.Login, direct OpenID Connect, or Brokered External Authentication.");
        }

        if (activeRegistrations.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                $"Authentication:Provider selects '{options.Provider}', but no matching authentication integration is registered.");
        }

        if (activeRegistrations[0] != options.Provider)
        {
            return ValidateOptionsResult.Fail(
                $"Authentication:Provider selects '{options.Provider}', but '{activeRegistrations[0]}' handlers are also registered.");
        }

        return ValidateOptionsResult.Success;
    }
}
