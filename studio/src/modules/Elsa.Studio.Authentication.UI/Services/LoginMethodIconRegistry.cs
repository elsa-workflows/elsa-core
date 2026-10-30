using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using MudBlazor;

namespace Elsa.Studio.Authentication.UI.Services;

public sealed class LoginMethodIconRegistry : ILoginMethodIconRegistry
{
    private static readonly LoginMethodIcon Fallback = new(Icons.Material.Outlined.Badge, "Identity provider");
    private readonly IReadOnlyDictionary<string, LoginMethodIcon> _icons;

    public LoginMethodIconRegistry(IEnumerable<ILoginMethodIconProvider> providers)
    {
        var registrations = providers.SelectMany(x => x.GetIcons()).ToArray();
        if (registrations.Any(x => string.IsNullOrWhiteSpace(x.IconId) || string.IsNullOrWhiteSpace(x.Icon.Svg)))
            throw new InvalidOperationException("Login method icons require an ID and a locally supplied SVG value.");

        var duplicate = registrations
            .GroupBy(x => x.IconId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Login method icon '{duplicate.Key}' is registered more than once.");

        _icons = registrations.ToDictionary(x => x.IconId, x => x.Icon, StringComparer.OrdinalIgnoreCase);
    }

    public LoginMethodIcon Resolve(string? iconId) =>
        iconId is not null && _icons.TryGetValue(iconId, out var icon) ? icon : Fallback;
}
