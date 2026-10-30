using Blazored.LocalStorage;
using Elsa.Studio.Login.BlazorServer.Services;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.BlazorServer.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds login services with Blazor Server implementations.
    /// </summary>
    public static IServiceCollection AddLoginModule(this IServiceCollection services)
    {
        // Add the login module.
        services.AddLoginModuleCore();
        
        // Register HttpContextAccessor.
        services.AddHttpContextAccessor();

        // Register Blazored LocalStorage.
        services.AddBlazoredLocalStorage();
        
        // Register JWT services.
        services.AddSingleton<IJwtParser, BlazorServerJwtParser>();
        services.AddScoped<IJwtAccessor, BlazorServerJwtAccessor>();

        services.AddScoped<IOpenIdConnectPkceStateService, BlazorServerOpenIdConnectPkceStateService>();
        
        return services;
    }
}