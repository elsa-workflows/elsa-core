using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Components;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Extensions;

/// <summary>WebAssembly registrations for Elsa Studio's public broker client.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a public broker client with browser-crypto PKCE and memory-only credentials by default.</summary>
    public static IServiceCollection AddExternalAuthenticationBroker(
        this IServiceCollection services,
        Action<ExternalAuthenticationWasmOptions> configure)
    {
        var options = new ExternalAuthenticationWasmOptions();
        configure(options);
        Validate(options);

        services.AddSingleton(new StudioAuthenticationProviderRegistration(StudioAuthenticationProvider.ExternalAuthentication));
        services.AddSingleton(options);
        services.AddSingleton(options.ToClientOptions());
        services.AddScoped<IExternalAuthenticationBrowserTokenStore, BrowserExternalAuthenticationTokenStore>();
        services.AddScoped<IExternalAuthenticationPkceTransactionStore, BrowserExternalAuthenticationPkceTransactionStore>();
        services.AddScoped<IExternalAuthenticationPkceService, BrowserExternalAuthenticationPkceService>();
        services.AddScoped<ExternalAuthenticationWasmTokenProvider>();
        services.AddScoped<IExternalAuthenticationTokenProvider>(provider => provider.GetRequiredService<ExternalAuthenticationWasmTokenProvider>());
        services.AddScoped<AuthenticationStateProvider, ExternalAuthenticationWasmAuthenticationStateProvider>();
        services.AddScoped<ExternalAuthenticationWasmCallbackService>();
        services.AddScoped<ExternalAuthenticationWasmLogoutService>();
        services.AddScoped<IExternalAuthenticationLoginCoordinator, ExternalAuthenticationWasmLoginCoordinator>();
        services.AddExternalAuthenticationLoginUI();
        services.AddScoped<IHttpConnectionOptionsConfigurator, ExternalAuthenticationHttpConnectionOptionsConfigurator>();
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<NavigateToExternalLogin>>();
        services.AddScoped<IFeature, ExternalAuthenticationBlazorWasmFeature>();
        services.AddAuthorizationCore();
        WarnForPersistentStorage(services, options);
        return services;
    }

    private static void Validate(ExternalAuthenticationWasmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("External Authentication requires a configured broker client ID.");
        if (!string.IsNullOrWhiteSpace(options.ClientSecret))
            throw new InvalidOperationException("Studio WebAssembly is a public client and must not contain a broker client secret.");
        if (!string.Equals(options.CallbackPath, ExternalAuthenticationCallbackPaths.SignIn, StringComparison.Ordinal) ||
            !string.Equals(options.LogoutCallbackPath, ExternalAuthenticationCallbackPaths.Logout, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"External Authentication uses the fixed Studio callback paths '{ExternalAuthenticationCallbackPaths.SignIn}' and '{ExternalAuthenticationCallbackPaths.Logout}'.");
        }
    }

    private static void WarnForPersistentStorage(IServiceCollection services, ExternalAuthenticationWasmOptions options)
    {
        if (options.BrowserStorage == ExternalAuthenticationBrowserStorageMode.Memory)
            return;

        services.AddSingleton<IStartupTask>(provider => new StorageWarningStartupTask(
            options.BrowserStorage,
            provider.GetRequiredService<ILogger<StorageWarningStartupTask>>()));
    }

    private sealed class StorageWarningStartupTask(
        ExternalAuthenticationBrowserStorageMode storageMode,
        ILogger<StorageWarningStartupTask> logger) : IStartupTask
    {
        public ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            logger.LogWarning(
                "External Authentication WebAssembly browser storage is set to {BrowserStorage}. Tokens persist beyond in-memory application state; assess XSS exposure before using this deployment option.",
                storageMode);
            return ValueTask.CompletedTask;
        }
    }
}
