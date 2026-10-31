using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.ExternalAuthentication.Components.LoginMethods;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Services;

public sealed class ExternalAuthenticationLoginMethodCatalog(IExternalAuthenticationLoginCoordinator coordinator) : ILoginMethodCatalog
{
    public async ValueTask<LoginMethodCatalogResult> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await coordinator.DiscoverAsync(cancellationToken);
        return new(response.Methods, response.PreferredMethodKey, coordinator.SecurityWarning);
    }
}

public sealed class ExternalLoginMethodComponentProvider : ILoginMethodComponentProvider
{
    public string Kind => "external";
    public Type ComponentType => typeof(ExternalLoginMethod);
}

public sealed class BrokerLocalLoginMethodComponentProvider : ILoginMethodComponentProvider
{
    public string Kind => "local";
    public Type ComponentType => typeof(BrokerLocalLoginMethod);
}

public sealed class BuiltInLoginMethodIconProvider : ILoginMethodIconProvider
{
    public IReadOnlyCollection<LoginMethodIconRegistration> GetIcons() =>
    [
        Icon("elsa", Icons.Material.Outlined.AccountCircle, "Elsa"),
        Icon("building", Icons.Material.Outlined.Business, "Organization"),
        Icon("github", Icons.Custom.Brands.GitHub, "GitHub"),
        Icon("microsoft", Icons.Material.Outlined.Window, "Microsoft"),
        Icon("google", Icons.Material.Outlined.Language, "Google"),
        Icon("facebook", Icons.Material.Outlined.Public, "Facebook"),
        Icon("x", Icons.Material.Outlined.AlternateEmail, "X")
    ];

    private static LoginMethodIconRegistration Icon(string id, string svg, string name) =>
        new(id, new(svg, name));
}

public static class ExternalAuthenticationLoginUiServiceCollectionExtensions
{
    public static IServiceCollection AddExternalAuthenticationLoginUI(this IServiceCollection services)
    {
        services.AddScoped<ILoginMethodCatalog, ExternalAuthenticationLoginMethodCatalog>();
        services.AddScoped<ILoginMethodComponentProvider, ExternalLoginMethodComponentProvider>();
        services.AddScoped<ILoginMethodComponentProvider, BrokerLocalLoginMethodComponentProvider>();
        services.AddSingleton<ILoginMethodIconProvider, BuiltInLoginMethodIconProvider>();
        return services;
    }
}
