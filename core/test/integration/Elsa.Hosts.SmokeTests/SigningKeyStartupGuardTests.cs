using Elsa.ModularServer.Web;
using Elsa.Server.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
/// These tests pin the shared contract: Production with a known-default signing key means a failed startup, for
/// both hosts.
/// </remarks>
public class SigningKeyStartupGuardTests
{
    [Fact]
    public void ClassicServerFailsToStartInProductionWithPlaceholderSigningKey() =>
        AssertBootFailsAtStartup<ClassicServerHost>();

    [Fact]
    public void ModularServerFailsToStartInProductionWithPlaceholderSigningKey() =>
        AssertBootFailsAtStartup<ModularServerHost>();

    private static void AssertBootFailsAtStartup<TEntryPoint>() where TEntryPoint : class
    {
        using var factory = new PlaceholderSigningKeyHostFixture<TEntryPoint>();

        // The WebApplicationFactory surfaces the host's own startup failure when it is first used.
        var exception = Assert.ThrowsAny<Exception>(() => factory.Services);

        Assert.IsType<OptionsValidationException>(exception.GetBaseException());
        Assert.Contains("SigningKey", exception.GetBaseException().Message);
    }
}

/// <summary>
/// Boots a host with the Production environment and its shipped appsettings, so the signing key remains the
/// known default that both hosts refuse outside Development/Demo.
/// </summary>
public class PlaceholderSigningKeyHostFixture<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}
