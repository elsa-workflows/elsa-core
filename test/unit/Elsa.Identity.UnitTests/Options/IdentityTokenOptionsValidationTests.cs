using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Options;

public class IdentityTokenOptionsValidationTests
{
    private const string SecureSigningKey = "test-signing-key-with-at-least-32-characters";

    public static IEnumerable<(Action<IdentityTokenOptions>, string)> InvalidConfigurations =>
    [
        (options => options.SigningKey = string.Empty, "SigningKey is required"),
        (options => options.SigningKey = " ", "SigningKey is required"),
        (options => options.SigningKey = $" {SecureSigningKey}", "must not contain leading or trailing whitespace"),
        (options => options.SigningKey = $"{SecureSigningKey} ", "must not contain leading or trailing whitespace"),
        (options => options.SigningKey = "short-signing-key", "at least 32 ASCII characters"),
        (options => options.SigningKey = new string('é', 32), "non-printable or non-ASCII characters"),
        (options => options.SigningKey = "sufficiently-large-secret-signing-key", "known public default"),
        (options => options.SigningKey = "CHANGE_ME_TO_A_SECURE_RANDOM_KEY", "known public default")
    ];

    [Test]
    public async Task AcceptsConfiguredSigningKey()
    {
        using var serviceProvider = CreateServiceProvider(options => options.SigningKey = SecureSigningKey);

        var options = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        await Assert.That(options.SigningKey).IsEqualTo(SecureSigningKey);
    }

    [Test]
    [Arguments("Development")]
    [Arguments("Demo")]
    public async Task AcceptsKnownDefaultSigningKeyInExplicitDemoOrDevelopmentMode(string environmentName)
    {
        using var serviceProvider = CreateServiceProvider(
            options => options.SigningKey = "CHANGE_ME_TO_A_SECURE_RANDOM_KEY",
            environmentName);

        var options = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        await Assert.That(options.SigningKey).IsEqualTo("CHANGE_ME_TO_A_SECURE_RANDOM_KEY");
    }

    [Test]
    [Arguments("sufficiently-large-secret-signing-key")]
    [Arguments("CHANGE_ME_TO_A_SECURE_RANDOM_KEY")]
    public async Task RejectsKnownDefaultSigningKeyInExplicitProductionMode(string knownDefaultKey)
    {
        using var serviceProvider = CreateServiceProvider(
            options => options.SigningKey = knownDefaultKey,
            "Production");

        var exception = Assert.ThrowsExactly<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains("known public default"));
    }

    [Test]
    [Arguments("sufficiently-large-secret-signing-key")]
    [Arguments("CHANGE_ME_TO_A_SECURE_RANDOM_KEY")]
    public async Task RejectsKnownDefaultSigningKeyDuringStartupValidationInExplicitProductionMode(string knownDefaultKey)
    {
        using var serviceProvider = CreateServiceProvider(
            options => options.SigningKey = knownDefaultKey,
            "Production");

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.ThrowsExactly<OptionsValidationException>(startupValidator.Validate);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains("known public default"));
    }

    [Test]
    [MethodDataSource(nameof(InvalidConfigurations))]
    public async Task RejectsInvalidSigningKey(Action<IdentityTokenOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var exception = Assert.ThrowsExactly<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains(expectedFailure));
    }

    [Test]
    [MethodDataSource(nameof(InvalidConfigurations))]
    public async Task RejectsInvalidSigningKeyDuringStartupValidation(Action<IdentityTokenOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.ThrowsExactly<OptionsValidationException>(startupValidator.Validate);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains(expectedFailure));
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