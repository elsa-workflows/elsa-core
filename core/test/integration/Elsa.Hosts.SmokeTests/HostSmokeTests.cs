using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Elsa.Hosts.SmokeTests;

/// <summary>
/// Boots a host the way its own entry point does and asserts it comes up serving gated routes.
/// </summary>
/// <remarks>
/// This repo runs two parallel feature systems -- the classic <c>Features/</c> path and the CShells
/// <c>ShellFeatures/</c> path -- and every module must register in both. Nothing exercised either until
/// now: the unit and integration suites construct services directly, so a service missing from one path,
/// or a feature registered in one and not the other, passes every test and fails only when a host starts.
/// Three such bugs in #7980 were found by running these two hosts by hand.
/// <para>
/// Each host is booted through <see cref="WebApplicationFactory{TEntryPoint}"/>, which runs the real
/// <c>Program</c> with its full feature registration. Assembling a service collection here instead would
/// reproduce exactly the blind spot these tests exist to close.
/// </para>
/// <para>
/// The assertions go through HTTP rather than by resolving services out of the container. The two hosts
/// have genuinely different container topologies -- the classic host is flat, while CShells gives each
/// shell its own provider, so the module services are simply not in the root one -- and a test that
/// reached into either would have to encode that difference and would break whenever CShells changed its
/// internals. Behaviour at the edge is both host-agnostic and the thing actually worth pinning: whichever
/// feature system a host uses, the observable result has to be the same.
/// </para>
/// </remarks>
public abstract class HostSmokeTests<TEntryPoint>(HostFixture<TEntryPoint> host) : IClassFixture<HostFixture<TEntryPoint>>
    where TEntryPoint : class
{
    /// <summary>
    /// Routes this host is expected to serve behind a permission. Each names a different module, so the
    /// set doubles as an inventory of what this host's feature system is supposed to have registered.
    /// </summary>
    protected abstract IReadOnlyCollection<string> GatedRoutes { get; }

    [Fact]
    public void HostStarts()
    {
        // Touching Services forces the host to be built, which is where a feature that fails to register or
        // an option that fails validation throws.
        Assert.NotNull(host.Services);
    }

    [Fact]
    public async Task EveryGatedRouteChallengesInsteadOfFailing()
    {
        using var client = host.CreateClient();
        var problems = new List<string>();

        foreach (var route in GatedRoutes)
        {
            var status = (int)(await client.GetAsync(route)).StatusCode;

            // Each way this can go wrong is a distinct bug, so they are named rather than collapsed into one
            // "expected 401" message that leaves the reader to work out which failure they are looking at.
            var problem = status switch
            {
                404 => "404: this host never registered the module serving it",
                >= 500 => $"{status}: the endpoint was found but could not be activated, so a dependency is missing",
                200 => "200: reachable without credentials, so no permission gate ran",
                401 => null,
                _ => $"{status}: expected 401"
            };

            if (problem is not null)
                problems.Add($"{route} -> {problem}");
        }

        // Every route is reported at once: when a feature system stops registering a group of modules, one
        // failure per run turns a single cause into a queue of identical-looking investigations.
        Assert.True(problems.Count == 0, $"{problems.Count} of {GatedRoutes.Count} gated route(s) did not challenge:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }
}

/// <summary>Boots a host once per test class.</summary>
public class HostFixture<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Both hosts refuse to start outside Development while the signing key is a known default. That is
        // the guard working as intended, so the test satisfies it rather than configuring around it.
        builder.UseEnvironment("Development");
    }
}
