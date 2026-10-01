using Elsa.Testing.Shared;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// FastEndpoints keeps one process-wide service resolver. <c>UseFastEndpoints()</c> points it at the host being built,
/// and <c>Factory.Create</c> reuses whichever resolver is set, so a class that starts an endpoint host and a class that
/// creates endpoints through the factory cannot run concurrently: the host's disposal would pull the container out from
/// under the factory, which then fails with <see cref="ObjectDisposedException"/>.
///
/// Sharing one collection makes those classes run one at a time, and <c>DisableParallelization</c> keeps them from
/// overlapping any other collection. Any future class that builds an endpoint host or uses <c>Factory.Create</c>
/// belongs in this collection too, and a host has to call <see cref="FastEndpointsResolver.Reset"/> once it is disposed.
/// </summary>
[CollectionDefinition(nameof(FastEndpointsCollection), DisableParallelization = true)]
public class FastEndpointsCollection;
