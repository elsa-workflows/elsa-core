namespace Elsa.UnitTests.Shared;

/// <summary>
/// Serializes the test classes of an assembly that depend on FastEndpoints' process-wide state: the service resolver
/// and <c>EndpointSecurityOptions.SecurityIsEnabled</c>. A host writes both while it is built and <c>Factory.Create</c>
/// reads the resolver, so running two such classes concurrently lets one class's host leak into, or be disposed under,
/// another class's test.
///
/// Sharing one collection makes those classes run one at a time, and <c>DisableParallelization</c> keeps them from
/// overlapping any other collection. Any class in an assembly this file is linked into that builds an endpoint host or
/// uses <c>Factory.Create</c> belongs in this collection, and one that builds a host also has to call
/// <see cref="FastEndpointsResolver.Reset"/> when it disposes that host.
/// </summary>
[CollectionDefinition(nameof(FastEndpointsCollection), DisableParallelization = true)]
public class FastEndpointsCollection;
