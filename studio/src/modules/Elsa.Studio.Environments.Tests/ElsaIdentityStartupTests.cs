using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.HostedServices;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// Covers the Host.Server ElsaIdentity path for #1062: startup must not throw, and
/// Feature init must tolerate ElsaIdentity's real token read when JS interop is unavailable.
/// </summary>
public class ElsaIdentityStartupTests
{
    [Fact]
    public async Task InitializeFeatures_DoesNotThrowWhenElsaIdentityTokenReadFails()
    {
        // Arrange
        var jwtAccessor = new ThrowingJwtAccessor();
        using var provider = BuildElsaIdentityServices(jwtAccessor, registerHostedService: false);

        // Act
        await provider.GetRequiredService<IFeatureService>().InitializeFeaturesAsync();

        // Assert
        Assert.True(jwtAccessor.ReadCount > 0);
        Assert.Empty(provider.GetRequiredService<IEnvironmentService>().Environments);
        Assert.NotEmpty(provider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task RunStartupTasksHostedService_DoesNotThrowWithEnvironmentsAndElsaIdentity()
    {
        // Arrange
        using var provider = BuildElsaIdentityServices(new ThrowingJwtAccessor(), registerHostedService: true);
        var hostedService = provider.GetServices<IHostedService>().OfType<RunStartupTasksHostedService>().Single();

        // Act
        var exception = await Record.ExceptionAsync(() => hostedService.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
        Assert.Empty(provider.GetRequiredService<IEnvironmentService>().Environments);
    }

    private static ServiceProvider BuildElsaIdentityServices(IJwtAccessor jwtAccessor, bool registerHostedService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddElsaIdentity();
        services.Replace(ServiceDescriptor.Scoped(_ => jwtAccessor));
        services.AddSingleton<IRemoteFeatureProvider, EmptyRemoteFeatureProvider>();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddEnvironmentsModule(backendApiConfig);
        services.AddTransient<ElsaIdentityAuthenticatingApiHttpMessageHandler>();

        if (registerHostedService)
            services.AddHostedService<RunStartupTasksHostedService>();

        return services.BuildServiceProvider();
    }

    private sealed class EmptyRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class ThrowingJwtAccessor : IJwtAccessor
    {
        public int ReadCount { get; private set; }

        public ValueTask<string?> ReadTokenAsync(string name)
        {
            ReadCount++;
            throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
        }

        public ValueTask WriteTokenAsync(string name, string token) => ValueTask.CompletedTask;

        public ValueTask ClearTokenAsync(string name) => ValueTask.CompletedTask;
    }
}
