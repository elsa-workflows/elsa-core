using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers identity token options validation.
    /// </summary>
    public static IServiceCollection AddIdentityTokenOptionsValidation(this IServiceCollection services)
    {
        services.AddOptions<IdentityTokenOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<IdentityTokenOptions>, Elsa.Extensions.ValidateIdentityTokenOptions>());
        return services;
    }
}

