using AspNetCore.Authentication.ApiKey;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShellDefaultAuthenticationFeature = Elsa.Identity.ShellFeatures.DefaultAuthenticationFeature;

namespace Elsa.Identity.UnitTests.ShellFeatures;

public class DefaultAuthenticationFeatureTests
{
    private readonly ShellDefaultAuthenticationFeature _feature = new();
    private readonly ServiceCollection _services = new();

    [Fact]
    public void UsesDefaultApiKeyProviderWhenAdminApiKeyIsNotConfigured()
    {
        using var serviceProvider = Activate();

        Assert.Equal(typeof(DefaultApiKeyProvider), _feature.ApiKeyProviderType);
        Assert.Equal(string.Empty, serviceProvider.GetRequiredService<IOptions<AdminApiKeyOptions>>().Value.ApiKey);
    }

    [Fact]
    public async Task UsesAdminApiKeyProviderWhenAdminApiKeyIsConfigured()
    {
        _feature.AdminApiKey = "configured-admin-api-key";

        using var serviceProvider = Activate();

        var provider = Assert.IsType<AdminApiKeyProvider>(serviceProvider.GetRequiredService<IApiKeyProvider>());
        var apiKey = await provider.ProvideAsync(_feature.AdminApiKey);

        Assert.Equal(typeof(AdminApiKeyProvider), _feature.ApiKeyProviderType);
        Assert.NotNull(apiKey);
    }

    [Fact]
    public async Task UsesAdminApiKeyProviderWhenDevelopmentAdminApiKeyIsEnabled()
    {
        _feature.UseDevelopmentAdminApiKey = true;

        using var serviceProvider = Activate();

        var provider = Assert.IsType<AdminApiKeyProvider>(serviceProvider.GetRequiredService<IApiKeyProvider>());
        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        Assert.Equal(typeof(AdminApiKeyProvider), _feature.ApiKeyProviderType);
        Assert.NotNull(apiKey);
    }

    private ServiceProvider Activate()
    {
        _feature.ConfigureServices(_services);
        return _services.BuildServiceProvider();
    }
}
