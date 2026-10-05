using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// The default environment must be selected before the remote feature catalog is fetched
/// so features are gated against that environment's catalog, not the fallback backend.
/// </summary>
public class FeatureCatalogTests
{
    [Fact]
    public Task InitializeFeatures_UsesDefaultCatalog_WhenEnvironmentsRegisteredFirst() =>
        AssertUsesDefaultCatalog(environmentsFirst: true);

    [Fact]
    public Task InitializeFeatures_UsesDefaultCatalog_WhenRemoteProviderRegisteredFirst() =>
        AssertUsesDefaultCatalog(environmentsFirst: false);

    [Fact]
    public async Task InitializeFeatures_FailedThenSucceeds_SecondPassUsesDefaultCatalog()
    {
        // Arrange
        var defaultOnly = new DefaultOnlyFeature();
        var fallbackOnly = new FallbackOnlyFeature();
        var handler = CreateCatalogHandler();
        handler.RemainingEnvironmentFailures = 1;
        using var provider = BuildServices(environmentsFirst: true, handler, defaultOnly, fallbackOnly);
        var features = provider.GetRequiredService<IFeatureService>();
        var environments = provider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();

        // Assert: one failed load; stay on the fallback catalog for this pass.
        Assert.Equal(1, handler.EnvironmentRequestCount);
        Assert.Null(environments.CurrentEnvironment);
        Assert.True(fallbackOnly.Initialized);
        Assert.False(defaultOnly.Initialized);

        // Act
        await features.InitializeFeaturesAsync();

        // Assert: the next pass loads Dev and gates against that catalog.
        Assert.Equal(2, handler.EnvironmentRequestCount);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.True(defaultOnly.Initialized);
    }

    // The dashboard relies on it to tell widgets that are still being registered from widgets that do not exist.
    [Fact]
    public async Task InitializeFeatures_IsReportedThroughTheEnvironmentAwareService()
    {
        using var provider = BuildServices(environmentsFirst: true, CreateCatalogHandler());
        var features = provider.GetRequiredService<IFeatureService>();
        Assert.False(features.IsInitialized);

        await features.InitializeFeaturesAsync();

        Assert.True(features.IsInitialized);
    }

    private static async Task AssertUsesDefaultCatalog(bool environmentsFirst)
    {
        // Arrange
        var defaultOnly = new DefaultOnlyFeature();
        var fallbackOnly = new FallbackOnlyFeature();
        var handler = CreateCatalogHandler();
        using var provider = BuildServices(environmentsFirst, handler, defaultOnly, fallbackOnly);
        var features = provider.GetRequiredService<IFeatureService>();
        var environments = provider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal(1, handler.EnvironmentRequestCount);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.Equal(new Uri("https://dev.example/"), environments.CurrentEnvironment?.Url);
        Assert.True(defaultOnly.Initialized);
        Assert.False(fallbackOnly.Initialized);
        Assert.Contains(handler.Requests, request =>
            request.RequestUri is not null
            && request.RequestUri.Host == "dev.example"
            && request.RequestUri.AbsolutePath.Contains("features", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(handler.Requests, request =>
            request.RequestUri is not null
            && request.RequestUri.Host == "backend.example"
            && request.RequestUri.AbsolutePath.Contains("features", StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceProvider BuildServices(
        bool environmentsFirst,
        EnvironmentsModuleTests.RecordingHandler handler,
        params IFeature[] extraFeatures)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(PassthroughAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);

        if (environmentsFirst)
        {
            services.AddEnvironmentsModule(backendApiConfig);
            services.AddScoped<IRemoteFeatureProvider, RemoteFeatureProvider>();
        }
        else
        {
            services.AddScoped<IRemoteFeatureProvider, RemoteFeatureProvider>();
            services.AddEnvironmentsModule(backendApiConfig);
        }

        foreach (var feature in extraFeatures)
            services.AddSingleton<IFeature>(feature);

        services.AddHttpClient(nameof(IEnvironmentsClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient(nameof(IFeaturesApi)).ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private static EnvironmentsModuleTests.RecordingHandler CreateCatalogHandler()
    {
        var handler = new EnvironmentsModuleTests.RecordingHandler
        {
            ResponseContentForRequest = request =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/environments", StringComparison.OrdinalIgnoreCase))
                {
                    return """
                        {
                          "environments": [
                            { "name": "Fallback", "url": "https://backend.example/" },
                            { "name": "Dev", "url": "https://dev.example/" }
                          ],
                          "defaultEnvironmentName": "Dev"
                        }
                        """;
                }

                if (url.Contains("dev.example", StringComparison.OrdinalIgnoreCase))
                {
                    return """
                        {
                          "items": [
                            { "fullName": "Elsa.DefaultOnly", "name": "DefaultOnly", "namespace": "Elsa" }
                          ],
                          "totalCount": 1
                        }
                        """;
                }

                return """
                    {
                      "items": [
                        { "fullName": "Elsa.FallbackOnly", "name": "FallbackOnly", "namespace": "Elsa" }
                      ],
                      "totalCount": 1
                    }
                    """;
            }
        };

        return handler;
    }

    [RemoteFeature("Elsa.DefaultOnly")]
    private sealed class DefaultOnlyFeature : IFeature
    {
        public bool Initialized { get; private set; }

        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return ValueTask.CompletedTask;
        }
    }

    [RemoteFeature("Elsa.FallbackOnly")]
    private sealed class FallbackOnlyFeature : IFeature
    {
        public bool Initialized { get; private set; }

        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PassthroughAuthenticationHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            base.SendAsync(request, cancellationToken);
    }
}
