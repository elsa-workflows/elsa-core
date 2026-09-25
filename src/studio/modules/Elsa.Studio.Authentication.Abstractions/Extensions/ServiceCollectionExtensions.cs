using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.Abstractions.Options;
using Elsa.Studio.Authentication.Abstractions.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables fail-fast validation for the authentication provider selected by the Studio host.
    /// </summary>
    public static IServiceCollection AddStudioAuthenticationMode(
        this IServiceCollection services,
        Action<StudioAuthenticationOptions> configure)
    {
        services.AddOptions<StudioAuthenticationOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<StudioAuthenticationOptions>, StudioAuthenticationOptionsValidator>());
        return services;
    }

    /// <summary>
    /// Records a provider-specific handler registration for mutual-exclusion validation.
    /// </summary>
    public static IServiceCollection AddStudioAuthenticationProviderRegistration(
        this IServiceCollection services,
        StudioAuthenticationProvider provider)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(new StudioAuthenticationProviderRegistration(provider)));
        return services;
    }
}
