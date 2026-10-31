using Elsa.Api.Client.Resources.ExternalAuthentication.Descriptors.Models;
using Elsa.Permissions;

namespace Elsa.ExternalAuthentication.IntegrationTests.Compatibility;

/// <summary>
/// Pins the permission descriptor the API serves to the one its client deserializes.
/// </summary>
/// <remarks>
/// The endpoint used to serve a descriptor private to the External Authentication module, keyed by a single
/// permission string. Moving it onto the core catalog changed the shape to a resource plus the verbs that
/// resource accepts, and the client model kept the old one — so it still deserialized, still compiled, and
/// handed callers blank identifiers with no way to reach the verbs. Nothing failed; the data just went
/// missing. Comparing the two property sets is what turns that back into a test failure.
/// </remarks>
public class PermissionDescriptorContractTests
{
    [Fact]
    public void ClientModelCarriesEveryFieldTheCatalogServes()
    {
        var served = typeof(PermissionDescriptor).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        var deserialized = typeof(ExternalAuthenticationPermissionDescriptor).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        // NonCoreVerbs is derived from SupportedVerbs on the server, so a client that has the verbs can
        // compute it and does not need it sent.
        served.Remove(nameof(PermissionDescriptor.NonCoreVerbs));

        var missing = served.Except(deserialized).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var unknown = deserialized.Except(served).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.True(missing.Length == 0, $"The client model would silently drop: {string.Join(", ", missing)}.");
        Assert.True(unknown.Length == 0, $"The client model expects fields the catalog does not serve, which will deserialize empty: {string.Join(", ", unknown)}.");
    }
}
