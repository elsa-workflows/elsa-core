using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Identity.UnitTests.Providers;

public class AdminCredentialProviderTests
{
    private readonly DefaultSecretHasher _secretHasher = new();

    [Fact]
    public async Task AdminApiKeyProviderDeniesDevelopmentApiKeyByDefault()
    {
        var provider = CreateAdminApiKeyProvider();

        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        Assert.Null(apiKey);
    }

    [Fact]
    public async Task AdminApiKeyProviderAcceptsDevelopmentApiKeyWhenExplicitlyConfigured()
    {
        var provider = CreateAdminApiKeyProvider(options => options.ApiKey = AdminApiKeyProvider.DevelopmentApiKey);

        var apiKey = await provider.ProvideAsync(AdminApiKeyProvider.DevelopmentApiKey);

        Assert.NotNull(apiKey);
        Assert.Equal("admin", apiKey.OwnerName);
        Assert.Contains(apiKey.Claims, claim => claim.Type == "permissions" && claim.Value == "*");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("anyone")]
    public async Task AdminUserProviderDeniesStaticPasswordByDefault(string userName)
    {
        var validator = CreateCredentialsValidator();

        var user = await validator.ValidateAsync(userName, "password");

        Assert.Null(user);
    }

    [Fact]
    public async Task AdminUserProviderAcceptsDevelopmentCredentialsWhenExplicitlyConfigured()
    {
        var validator = CreateCredentialsValidator(options =>
        {
            options.UserName = "admin";
            options.Password = "password";
        });

        var user = await validator.ValidateAsync("admin", "password");

        Assert.NotNull(user);
        Assert.Equal("admin", user.Name);
    }

    [Fact]
    public async Task AdminUserProviderDeniesArbitraryUsernameWhenDevelopmentCredentialsAreConfigured()
    {
        var validator = CreateCredentialsValidator(options =>
        {
            options.UserName = "admin";
            options.Password = "password";
        });

        var user = await validator.ValidateAsync("anyone", "password");

        Assert.Null(user);
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
