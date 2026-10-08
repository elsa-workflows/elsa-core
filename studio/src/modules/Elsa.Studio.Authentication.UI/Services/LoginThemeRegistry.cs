using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Services;

/// <summary>
/// Resolves the single startup-selected theme without giving themes access to authentication behavior.
/// </summary>
public sealed class LoginThemeRegistry : ILoginThemeRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public LoginThemeRegistry(
        IEnumerable<LoginThemeRegistration> registrations,
        IOptions<LoginThemeOptions> options,
        IOptions<StudioThemeOptions> studioThemeOptions,
        IServiceProvider serviceProvider)
    {
        var result = LoginThemeRegistrationRules.ValidateAndSelect(
            registrations,
            options.Value.Theme,
            studioThemeOptions.Value.Theme);
        if (result.Errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", result.Errors));

        Selected = result.Selected!;
        _serviceProvider = serviceProvider;
    }

    public LoginThemeRegistration Selected { get; }

    public ILoginThemeProvider ResolveSelectedProvider() =>
        _serviceProvider.GetRequiredService(Selected.ProviderType) as ILoginThemeProvider
        ?? throw new InvalidOperationException(
            $"Login theme '{Selected.Id}' resolved a provider that does not implement {nameof(ILoginThemeProvider)}.");
}
