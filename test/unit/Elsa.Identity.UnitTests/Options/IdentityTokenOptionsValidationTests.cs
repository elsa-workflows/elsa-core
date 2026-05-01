using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.UnitTests.Options;

public class IdentityTokenOptionsValidationTests
{
    public static TheoryData<Action<IdentityTokenOptions>> InvalidConfigurations => new()
    {
        options => options.SigningKey = string.Empty,
        options => options.SigningKey = " ",
    };

    [Fact]
    public void AcceptsConfiguredSigningKey()
    {
        using var serviceProvider = CreateServiceProvider(options => options.SigningKey = "test-signing-key");

        var options = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        Assert.Equal("test-signing-key", options.SigningKey);
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsMissingSigningKey(Action<IdentityTokenOptions> configure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var exception = Assert.Throws<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);

        Assert.Contains("SigningKey is required", exception.Failures);
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsMissingSigningKeyDuringStartupValidation(Action<IdentityTokenOptions> configure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.Throws<OptionsValidationException>(startupValidator.Validate);

        Assert.Contains("SigningKey is required", exception.Failures);
    }

    private static ServiceProvider CreateServiceProvider(Action<IdentityTokenOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddIdentityTokenOptionsValidation();
        services.Configure(configure ?? (_ => { }));
        return services.BuildServiceProvider();
    }
}

