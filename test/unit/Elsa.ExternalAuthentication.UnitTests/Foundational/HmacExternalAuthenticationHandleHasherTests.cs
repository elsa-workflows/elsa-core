using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class HmacExternalAuthenticationHandleHasherTests
{
    [Fact]
    public void ConfiguredSharedKeyProducesStableHashesAcrossNodes()
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

        Assert.Equal(firstNode.Hash("opaque-handle"), secondNode.Hash("opaque-handle"));
        Assert.Equal(firstNode.Hash("issuer\u001fsubject"), secondNode.Hash("issuer\u001fsubject"));
    }

    [Fact]
    public void ProcessLocalFallbackDoesNotCreateAClusterWideKey()
    {
        using var firstNode = new HmacExternalAuthenticationHandleHasher();
        using var secondNode = new HmacExternalAuthenticationHandleHasher();

        Assert.NotEqual(firstNode.Hash("opaque-handle"), secondNode.Hash("opaque-handle"));
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("c2hvcnQ=")]
    public void ValidatorRejectsInvalidSharedKeys(string sharedKey)
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
        var validator = new ExternalAuthenticationOptionsValidator(Microsoft.Extensions.Options.Options.Create(extensions));

        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("SharedKeyBase64", StringComparison.Ordinal));
    }
}
