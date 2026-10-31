using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Services;

/// <summary>
/// Resolves the startup-selected Elsa Studio theme pack.
/// </summary>
public sealed class StudioThemeRegistry : IStudioThemeRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public StudioThemeRegistry(
        IEnumerable<StudioThemeRegistration> registrations,
        IOptions<StudioThemeOptions> options,
        IServiceProvider serviceProvider)
    {
        var result = StudioThemeRegistrationRules.ValidateAndSelect(registrations, options.Value.Theme);
        if (result.Errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", result.Errors));

        Selected = result.Selected!;
        _serviceProvider = serviceProvider;
    }

    public StudioThemeRegistration Selected { get; }

    public IThemeProvider ResolveSelectedProvider() =>
        _serviceProvider.GetRequiredService(Selected.ProviderType) as IThemeProvider
        ?? throw new InvalidOperationException(
            $"Studio theme '{Selected.Id}' resolved a provider that does not implement {nameof(IThemeProvider)}.");
}
