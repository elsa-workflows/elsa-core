using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.UnitTests.Options;

public class IdentityTokenOptionsValidationTests
{
    private const string SecureSigningKey = "test-signing-key-with-at-least-32-characters";

    public static TheoryData<Action<IdentityTokenOptions>, string> InvalidConfigurations => new()
    {
        { options => options.SigningKey = string.Empty, "SigningKey is required" },
        { options => options.SigningKey = " ", "SigningKey is required" },
        { options => options.SigningKey = $" {SecureSigningKey}", "must not contain leading or trailing whitespace" },
        { options => options.SigningKey = $"{SecureSigningKey} ", "must not contain leading or trailing whitespace" },
        { options => options.SigningKey = "short-signing-key", "at least 32 ASCII characters" },
        { options => options.SigningKey = new string('é', 32), "non-printable or non-ASCII characters" },
        { options => options.SigningKey = "sufficiently-large-secret-signing-key", "known public default" },
        { options => options.SigningKey = "CHANGE_ME_TO_A_SECURE_RANDOM_KEY", "known public default" },
        { options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens", "known public default" },
        { options => options.SigningKey = "LARGE-SIGNING-KEY-FOR-SIGNING-JWT-TOKENS", "known public default" },
    };

    [Fact]
    public void AcceptsConfiguredSigningKey()
    {
        using var serviceProvider = CreateServiceProvider(options => options.SigningKey = SecureSigningKey);

        var options = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        Assert.Equal(SecureSigningKey, options.SigningKey);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Demo")]
    public void AcceptsKnownDefaultSigningKeyInExplicitDemoOrDevelopmentMode(string environmentName)
    {
        AssertAcceptedInEnvironment("CHANGE_ME_TO_A_SECURE_RANDOM_KEY", environmentName);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Demo")]
    public void AcceptsSampleAppSigningKeyInExplicitDemoOrDevelopmentMode(string environmentName)
    {
        AssertAcceptedInEnvironment("large-signing-key-for-signing-JWT-tokens", environmentName);
    }

    [Fact]
    public void RejectsMissingSigningKeyWhenNoneIsConfigured()
    {
        using var serviceProvider = CreateServiceProvider(environmentName: "Production");

        var exception = Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IStartupValidator>().Validate);

        Assert.Contains(exception.Failures, failure => failure.Contains("SigningKey is required"));
    }

    private static void AssertAcceptedInEnvironment(string signingKey, string environmentName)
    {
        using var serviceProvider = CreateServiceProvider(options => options.SigningKey = signingKey, environmentName);

        var options = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        Assert.Equal(signingKey, options.SigningKey);
    }

    [Theory]
    [InlineData("sufficiently-large-secret-signing-key")]
    [InlineData("CHANGE_ME_TO_A_SECURE_RANDOM_KEY")]
    [InlineData("large-signing-key-for-signing-JWT-tokens")]
    public void RejectsKnownDefaultSigningKeyInExplicitProductionMode(string knownDefaultKey)
    {
        using var serviceProvider = CreateServiceProvider(
            options => options.SigningKey = knownDefaultKey,
            "Production");

        var exception = Assert.Throws<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("known public default"));
    }

    [Theory]
    [InlineData("sufficiently-large-secret-signing-key")]
    [InlineData("CHANGE_ME_TO_A_SECURE_RANDOM_KEY")]
    [InlineData("large-signing-key-for-signing-JWT-tokens")]
    public void RejectsKnownDefaultSigningKeyDuringStartupValidationInExplicitProductionMode(string knownDefaultKey)
    {
        using var serviceProvider = CreateServiceProvider(
            options => options.SigningKey = knownDefaultKey,
            "Production");

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.Throws<OptionsValidationException>(startupValidator.Validate);

        Assert.Contains(exception.Failures, failure => failure.Contains("known public default"));
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsInvalidSigningKey(Action<IdentityTokenOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var exception = Assert.Throws<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains(expectedFailure));
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsInvalidSigningKeyDuringStartupValidation(Action<IdentityTokenOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.Throws<OptionsValidationException>(startupValidator.Validate);

        Assert.Contains(exception.Failures, failure => failure.Contains(expectedFailure));
    }

    private static ServiceProvider CreateServiceProvider(Action<IdentityTokenOptions>? configure = null, string? environmentName = null)
    {
        var services = new ServiceCollection();

        if (environmentName is not null)
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environmentName));

        services.AddIdentityTokenOptionsValidation();
        services.Configure(configure ?? (_ => { }));
        return services.BuildServiceProvider();
    }

    private class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(IdentityTokenOptionsValidationTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
