using Elsa.ModularServer.Web;
using Elsa.Server.Web;

namespace Elsa.Hosts.SmokeTests;

/// <summary>Elsa.Server.Web, which registers its modules through the classic <c>Features/</c> path.</summary>
public class ClassicHostSmokeTests(HostFixture<ClassicServerHost> host) : HostSmokeTests<ClassicServerHost>(host)
{
    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GatedRoutes =>
    [
        "/elsa/api/workflow-definitions",
        "/elsa/api/workflow-instances",
        "/elsa/api/identity/roles",
        "/elsa/api/identity/permissions",
        "/elsa/api/dashboard/overview"
    ];
}

/// <summary>Elsa.ModularServer.Web, which registers its modules through the CShells <c>ShellFeatures/</c> path.</summary>
public class ShellHostSmokeTests(HostFixture<ModularServerHost> host) : HostSmokeTests<ModularServerHost>(host)
{
    /// <inheritdoc />
    /// <remarks>
    /// The two route sets overlap but are not identical: each lists what its own host actually configures,
    /// and External Authentication is enabled only here. Keeping them separate is the point -- a route that
    /// disappears from one host and not the other is the divergence these tests are looking for.
    /// </remarks>
    protected override IReadOnlyCollection<string> GatedRoutes =>
    [
        "/elsa/api/workflow-definitions",
        "/elsa/api/workflow-instances",
        "/elsa/api/identity/permissions",
        "/elsa/api/external-authentication/connections",
        "/elsa/api/external-authentication/descriptors/adapters"
    ];
}
