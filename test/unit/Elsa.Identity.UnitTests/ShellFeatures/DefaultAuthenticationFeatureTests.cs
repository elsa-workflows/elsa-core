using AspNetCore.Authentication.ApiKey;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShellDefaultAuthenticationFeature = Elsa.Identity.ShellFeatures.DefaultAuthenticationFeature;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.ShellFeatures;

public class DefaultAuthenticationFeatureTests
{
    private readonly ShellDefaultAuthenticationFeature _feature = new();
    private readonly ServiceCollection _services = new();

    [Test]
    public async Task UsesDefaultApiKeyProviderWhenAdminApiKeyIsNotConfigured()
    {
        using var serviceProvider = Activate();

        await Assert.That(_feature.ApiKeyProviderType).IsEqualTo(typeof(DefaultApiKeyProvider));
        await Assert.That(serviceProvider.GetRequiredService<IOptions<AdminApiKeyOptions>>().Value.ApiKey).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task UsesAdminApiKeyProviderWhenAdminApiKeyIsConfigured()
    {
        _feature.AdminApiKey = "configured-admin-api-key";

        using var serviceProvider = Activate();

        var resolvedProvider = serviceProvider.GetRequiredService<IApiKeyProvider>();
        await Assert.That(resolvedProvider).IsOfType(typeof(AdminApiKeyProvider));
        var provider = (AdminApiKeyProvider)resolvedProvider;
        var apiKey = await provider.ProvideAsync(_feature.AdminApiKey);

        await Assert.That(_feature.ApiKeyProviderType).IsEqualTo(typeof(AdminApiKeyProvider));
        await Assert.That(apiKey).IsNotNull();
    }

    [Test]
    public async Task UsesAdminApiKeyProviderWhenDevelopmentAdminApiKeyIsEnabled()
    {
        _feature.UseDevelopmentAdminApiKey = true;

        using var serviceProvider = Activate();

        var resolvedProvider = serviceProvider.GetRequiredService<IApiKeyProvider>();
        await Assert.That(resolvedProvider).IsOfType(typeof(AdminApiKeyProvider));
        var provider = (AdminApiKeyProvider)resolvedProvider;
        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        await Assert.That(_feature.ApiKeyProviderType).IsEqualTo(typeof(AdminApiKeyProvider));
        await Assert.That(apiKey).IsNotNull();
    }

    private ServiceProvider Activate()
    {
        _feature.ConfigureServices(_services);
        return _services.BuildServiceProvider();
    }
}