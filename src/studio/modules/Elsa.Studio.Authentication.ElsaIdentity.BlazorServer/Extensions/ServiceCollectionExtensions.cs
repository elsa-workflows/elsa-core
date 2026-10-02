using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;

/// <summary>
/// Service registrations for ElsaIdentity in Blazor Server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ElsaIdentity services with Blazor Server implementations.
    /// </summary>
    public static IServiceCollection AddElsaIdentity(this IServiceCollection services)
    {
        services.AddElsaIdentityCore();
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtAccessor, BlazorServerJwtAccessor>();

        return services;
    }
}

