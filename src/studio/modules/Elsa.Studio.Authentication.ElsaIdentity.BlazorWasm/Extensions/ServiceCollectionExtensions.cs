using Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm.Services;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm.Extensions;

/// <summary>
/// Service registrations for ElsaIdentity in Blazor WebAssembly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ElsaIdentity services with Blazor WebAssembly implementations.
    /// </summary>
    public static IServiceCollection AddElsaIdentity(this IServiceCollection services)
    {
        services.AddElsaIdentityCore();
        services.AddScoped<IJwtAccessor, BlazorWasmJwtAccessor>();

        return services;
    }
}

