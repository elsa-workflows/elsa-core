using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Providers;

public class AdminCredentialProviderTests
{
    private readonly DefaultSecretHasher _secretHasher = new();

    [Test]
    public async Task AdminApiKeyProviderDeniesDevelopmentApiKeyByDefault()
    {
        var provider = CreateAdminApiKeyProvider();

        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        await Assert.That(apiKey).IsNull();
    }

    [Test]
    public async Task AdminApiKeyProviderAcceptsDevelopmentApiKeyWhenExplicitlyConfigured()
    {
        var provider = CreateAdminApiKeyProvider(options => options.ApiKey = AdminApiKeyProvider.DevelopmentApiKey);

        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        await Assert.That(apiKey).IsNotNull();
        await Assert.That(apiKey.OwnerName).IsEqualTo("admin");
        await Assert.That(apiKey.Claims).Contains(claim => claim.Type == "permissions" && claim.Value == "*");
    }

    [Test]
    [Arguments("admin")]
    [Arguments("anyone")]
    public async Task AdminUserProviderDeniesStaticPasswordByDefault(string userName)
    {
        var validator = CreateCredentialsValidator();

        var user = await validator.ValidateAsync(userName, "password");

        await Assert.That(user).IsNull();
    }

    [Test]
    public async Task AdminUserProviderAcceptsDevelopmentCredentialsWhenExplicitlyConfigured()
    {
        var validator = CreateCredentialsValidator(options =>
        {
            options.UserName = "admin";
            options.Password = "password";
        });

        var user = await validator.ValidateAsync("admin", "password");

        await Assert.That(user).IsNotNull();
        await Assert.That(user.Name).IsEqualTo("admin");
    }

    [Test]
    public async Task AdminUserProviderDeniesArbitraryUsernameWhenDevelopmentCredentialsAreConfigured()
    {
        var validator = CreateCredentialsValidator(options =>
        {
            options.UserName = "admin";
            options.Password = "password";
        });

        var user = await validator.ValidateAsync("anyone", "password");

        await Assert.That(user).IsNull();
    }

    private static AdminApiKeyProvider CreateAdminApiKeyProvider(Action<AdminApiKeyOptions>? configure = null)
    {
        var options = new AdminApiKeyOptions();
        configure?.Invoke(options);

        return new(OptionsFactory.Create(options));
    }

    private DefaultUserCredentialsValidator CreateCredentialsValidator(Action<AdminUserProviderOptions>? configure = null)
    {
        var options = new AdminUserProviderOptions();
        configure?.Invoke(options);

        var userProvider = new AdminUserProvider(_secretHasher, OptionsFactory.Create(options));
        return new(userProvider, _secretHasher);
    }
}