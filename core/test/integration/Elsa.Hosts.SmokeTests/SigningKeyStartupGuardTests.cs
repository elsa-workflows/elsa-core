using System.Security.Cryptography;
using Elsa.ModularServer.Web;
using Elsa.Server.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Hosts.SmokeTests;

/// <summary>
/// Both hosts ship a known-default value as their SigningKey in appsettings.json, and must refuse to start in
/// Production while it is in place, rather than booting into per-request failures.
/// </summary>
/// <remarks>
/// Elsa.Server.Web fails at startup because its feature registration lands on the host container, whose
/// IStartupValidator the host runs while starting. Elsa.ModularServer.Web registers the same validation inside
/// each CShells shell container, and those containers are built lazily on the first request - issue #9106: the
/// host used to boot with the placeholder in Production and only surfaced the error as a 500 on the first login.
/// These tests pin the shared contract: Production with a known-default signing key means a failed startup for
/// both hosts, while a real signing key, disabling the identity features or the Demo environment all start as
/// before.
/// </remarks>
public class SigningKeyStartupGuardTests
{
    [Fact]
    public void ClassicServerFailsToStartInProductionWithPlaceholderSigningKey() =>
        AssertBootFailsAtStartup<ClassicServerHost>();

    [Fact]
    public void ModularServerFailsToStartInProductionWithPlaceholderSigningKey() =>
        AssertBootFailsAtStartup<ModularServerHost>();

    [Fact]
    public void ModularServerStartsInProductionWithValidSigningKey()
    {
        // A key that is not a known default and survives the validator's rules must boot normally. The in-memory
        // override adds only the leaf key on top of the shipped appsettings, leaving CShells' object-map
        // structure (and every other feature entry) exactly as shipped.
        AssertBootStartsAtStartup<ModularServerHost>(configuration: new Dictionary<string, string>
        {
            ["CShells:Shells:Default:Features:Identity:SigningKey"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        });
    }

    [Fact]
    public void ModularServerStartsInProductionWhenIdentityFeaturesAreDisabled()
    {
        // With the identity features disabled there is nothing to validate and the placeholder key stays untouched,
        // matching how the shell assembles itself: no identity options are registered, so no signing key is needed.
        AssertBootStartsAtStartup<ModularServerHost>(settings: new Dictionary<string, string>
        {
            ["CShells:Shells:Default:Features:Identity"] = "false",
            ["CShells:Shells:Default:Features:DefaultAuthentication"] = "false",
        });
    }

    [Fact]
    public void ModularServerStartsInDemoWithPlaceholderSigningKey()
    {
        // Known-default keys keep being accepted in the Demo environment, same leniency Development gets.
        AssertBootStartsAtStartup<ModularServerHost>(environment: "Demo");
    }

    private static void AssertBootFailsAtStartup<TEntryPoint>() where TEntryPoint : class
    {
        using var factory = new PlaceholderSigningKeyHostFixture<TEntryPoint>();

        // The WebApplicationFactory surfaces the host's own startup failure when it is first used.
        var exception = Assert.ThrowsAny<Exception>(() => factory.Services);

        Assert.IsType<OptionsValidationException>(exception.GetBaseException());
        Assert.Contains("SigningKey", exception.GetBaseException().Message);
    }

    private static void AssertBootStartsAtStartup<TEntryPoint>(string? environment = null, IReadOnlyDictionary<string, string>? settings = null, IReadOnlyDictionary<string, string>? configuration = null) where TEntryPoint : class
    {
        using var factory = new PlaceholderSigningKeyHostFixture<TEntryPoint>(environment, settings, configuration);

        // Resolving the service provider builds and starts the host, and the preflight validates before the server
        // listens, so any startup failure surfaces as an exception here rather than a later request failure.
        Assert.NotNull(factory.Services);
    }
}

/// <summary>
/// Boots a host in Production with its shipped appsettings, so the signing key remains the known default that
/// both hosts refuse outside Development/Demo. The optional parameters carry launch-argument-style settings
/// (the same <c>UseSetting</c> channel other host test fixtures use) and additional app configuration keys that
/// ride an in-memory source on top of the shipped appsettings, for the cases that are expected to start rather
/// than fail.
/// </summary>
public class PlaceholderSigningKeyHostFixture<TEntryPoint>(
    string? environment = null,
    IReadOnlyDictionary<string, string>? settings = null,
    IReadOnlyDictionary<string, string>? configuration = null) : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment ?? "Production");

        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }

        if (configuration is not null)
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(configuration);
            });
        }
    }
}
