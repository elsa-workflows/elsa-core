using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// Environments used to take <see cref="IPermissionSnapshotCache"/>, which is implemented by
/// <see cref="IdentityPermissionContext"/> and needs the backend client whose accessor needs the
/// environment service. Resolving either service then hung. The refresh signal breaks that cycle.
/// </summary>
public sealed class CombinedModuleHostTests
{
    [Fact]
    public async Task CombinedEnvironmentsAndSecurityHost_ResolvesWithinTimeout()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddSingleton<IRemoteFeatureProvider, NoRemoteFeatures>();

        var backend = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/")
        };
        services.AddRemoteBackend(backend);
        services.AddEnvironmentsModule(backend);
        services.AddSecurityModule(backend);

        await using var provider = services.BuildServiceProvider();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        await Task.Run(() =>
        {
            using var scope = provider.CreateScope();
            var environment = scope.ServiceProvider.GetRequiredService<IEnvironmentService>();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionSnapshotCache>();
            var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            Assert.IsType<DefaultEnvironmentService>(environment);
            Assert.IsType<IdentityPermissionContext>(cache);
            Assert.NotNull(permissions);
        }, timeout.Token).WaitAsync(timeout.Token);
    }

    private sealed class NoRemoteFeatures : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }
}
