using Elsa.SasTokens.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.SasTokens.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SAS tokens module to the service collection.
    /// </summary>
    public static IServiceCollection AddSasTokens(this IServiceCollection services, Func<IServiceProvider, IDataProtectionProvider> dataProtectionProvider)
    {
        services.AddSingleton<ITokenService>(sp =>
        {
            var protectionProvider = dataProtectionProvider(sp);
            return new DataProtectorTokenService(protectionProvider);
        });
        return services;
    }
}