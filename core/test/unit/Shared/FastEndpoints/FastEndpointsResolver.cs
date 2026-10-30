using System.Runtime.CompilerServices;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UnitTests.Shared;

/// <summary>
/// <c>UseFastEndpoints()</c> points FastEndpoints' process-wide service resolver at the host's container and nothing
/// points it away again, so a disposed test host leaves a resolver behind that throws
/// <see cref="ObjectDisposedException"/> for whatever touches it next. <c>Factory.Create</c> does: it only installs a
/// resolver of its own when none is set.
///
/// A test class that starts an endpoint host therefore calls <see cref="Reset"/> when it disposes that host.
/// </summary>
/// <remarks>
/// Linked into the test projects that need it rather than living in <c>Elsa.Testing.Shared</c>: that library also
/// targets frameworks that resolve a FastEndpoints version without <c>UseMessaging()</c>, the only public API that
/// replaces the resolver.
/// </remarks>
internal static class FastEndpointsResolver
{
    // Never disposed, so the resolver it backs stays usable for the rest of the test run. The empty list of
    // discovered types keeps FastEndpoints from scanning every loaded assembly for message handlers.
    private static readonly IServiceProvider Services = new ServiceCollection()
        .AddHttpContextAccessor()
        .AddMessaging(new List<Type>())
        .BuildServiceProvider();

    /// <summary>
    /// Points the process-wide resolver at a container that outlives every test host. Also runs when the test assembly
    /// loads, so <c>Factory.Create</c> finds the same resolver whether or not a host ran before it. That resolver is
    /// not the unit-test one <c>Factory.Create</c> would install by itself: its <c>CreateScope()</c> creates a scope
    /// from this container rather than from the services registered on the test's <c>HttpContext</c>.
    /// </summary>
    [ModuleInitializer]
    public static void Reset() => Services.UseMessaging();
}
