using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared;

/// <summary>
/// <c>UseFastEndpoints()</c> points FastEndpoints' process-wide service resolver at the host's container and nothing
/// points it away again, so a disposed test host leaves a resolver behind that throws
/// <see cref="ObjectDisposedException"/> for whatever touches it next. <c>Factory.Create</c> does: it only installs a
/// resolver of its own when none is set.
///
/// A test class that starts an endpoint host calls <see cref="Reset"/> once the host is disposed. Because the resolver
/// is process-global, those classes and the ones using <c>Factory.Create</c> also have to share a non-parallel xunit
/// collection, or a host could replace or dispose the resolver in the middle of another class's test.
/// </summary>
/// <remarks>
/// Linked into the test projects that need it rather than living in <c>Elsa.Testing.Shared</c>: that library also
/// targets frameworks that resolve a FastEndpoints version without <c>UseMessaging()</c>, the only public API that
/// replaces the resolver.
/// </remarks>
internal static class FastEndpointsResolver
{
    // Never disposed, so the resolver it backs stays usable for the rest of the test run.
    private static readonly IServiceProvider Services = new ServiceCollection()
        .AddHttpContextAccessor()
        .AddMessaging(new List<Type>())
        .BuildServiceProvider();

    /// <summary>Points the process-wide resolver at a container that outlives every test host.</summary>
    public static void Reset() => Services.UseMessaging();
}
