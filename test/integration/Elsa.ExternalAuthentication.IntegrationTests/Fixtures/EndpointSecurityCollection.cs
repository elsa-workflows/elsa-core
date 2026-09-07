namespace Elsa.ExternalAuthentication.IntegrationTests.Fixtures;

/// <summary>
/// <see cref="EndpointSecurityOptions.SecurityIsEnabled"/> is a process-global mutable static that FastEndpoints
/// reads once per host, while <c>UseFastEndpoints()</c> configures the endpoints. Every test class in this assembly
/// that builds an endpoint host has to set it — five of them to <c>false</c> and
/// <c>IdentityLinkAuthorizationTests</c> to <c>true</c> — and restore it afterwards, so running any two of them
/// concurrently lets one class's value leak into another's endpoint configuration: endpoints meant to be anonymous
/// come back carrying <c>Permissions(...)</c> and answer 401/403, and the authorization test's endpoints come back
/// <c>AllowAnonymous</c> and stop enforcing the very thing it asserts.
///
/// Sharing one collection makes those classes run one at a time, and <c>DisableParallelization</c> keeps them from
/// overlapping any other collection. Any future class that builds an endpoint host belongs in this collection too.
/// </summary>
[CollectionDefinition(nameof(EndpointSecurityCollection), DisableParallelization = true)]
public class EndpointSecurityCollection;
