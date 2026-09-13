using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class HmacExternalAuthenticationHandleHasherTests
{
    [Test]
    public async Task ConfiguredSharedKeyProducesStableHashesAcrossNodes()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions
        {
            HandleHashing = new ExternalAuthenticationHandleHashingOptions
            {
                SharedKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray())
            }
        });

        using var firstNode = new HmacExternalAuthenticationHandleHasher(options);
        using var secondNode = new HmacExternalAuthenticationHandleHasher(options);

        await Assert.That(secondNode.Hash("opaque-handle")).IsEqualTo(firstNode.Hash("opaque-handle"));
        await Assert.That(secondNode.Hash("issuer\u001fsubject")).IsEqualTo(firstNode.Hash("issuer\u001fsubject"));
    }

    [Test]
    public async Task ProcessLocalFallbackDoesNotCreateAClusterWideKey()
    {
        using var firstNode = new HmacExternalAuthenticationHandleHasher();
        using var secondNode = new HmacExternalAuthenticationHandleHasher();

        await Assert.That(secondNode.Hash("opaque-handle")).IsNotEqualTo(firstNode.Hash("opaque-handle"));
    }

    [Test]
    [Arguments("not-base64")]
    [Arguments("c2hvcnQ=")]
    public async Task ValidatorRejectsInvalidSharedKeys(string sharedKey)
    {
        var options = new ExternalAuthenticationOptions
        {
            HandleHashing = new ExternalAuthenticationHandleHashingOptions { SharedKeyBase64 = sharedKey }
        };

        var extensions = new ExternalAuthenticationExtensionOptions();
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.Adapter, "oidc"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "reject"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "create-user"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "elsa-roles"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "claim-mapping"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "group-mapping"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "claim-pass-through"));
        var validator = new ExternalAuthenticationOptionsValidator(Microsoft.Extensions.Options.Options.Create(extensions), NullLogger<ExternalAuthenticationOptionsValidator>.Instance);

        var result = validator.Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("SharedKeyBase64", StringComparison.Ordinal));
    }
}
