using System.Runtime.CompilerServices;

namespace Elsa.Shells.Api.Tests;

/// <summary>
/// <see cref="EndpointSecurityOptions.SecurityIsEnabled"/> is process-global mutable state that FastEndpoints reads
/// once per host, while <c>UseFastEndpoints()</c> configures the endpoints. xUnit runs the test classes in this
/// assembly in parallel, so a per-test save/restore of that global races: one class restoring the flag to
/// <c>true</c> between another class's write and its <c>UseFastEndpoints()</c> call produces endpoints carrying
/// authorization metadata in a pipeline that has no <c>UseAuthorization</c>, which fails the request with
/// "contains authorization metadata, but a middleware was not found that supports authorization".
///
/// Every test in this assembly wants security disabled, so set it once before any test runs and never touch it again.
/// </summary>
internal static class TestSecurityDefaults
{
    [ModuleInitializer]
    internal static void DisableEndpointSecurity() => EndpointSecurityOptions.SecurityIsEnabled = false;
}
