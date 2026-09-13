using Elsa.Secrets.Options;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.UnitTests;

public class SecretValueProtectorTests
{
    [Test]
    public async Task Protect_AndUnprotect_RoundTripsValue()
    {
        var protector = CreateProtector("0123456789abcdef0123456789abcdef"u8.ToArray());

        var protectedValue = protector.Protect("plain-text-secret");
        var value = protector.Unprotect(protectedValue);

        await Assert.That(protectedValue).IsNotEqualTo("plain-text-secret");
        await Assert.That(value).IsEqualTo("plain-text-secret");
    }

    [Test]
    [Arguments(null)]
    [Arguments(0)]
    [Arguments(15)]
    [Arguments(17)]
    public async Task Protect_RejectsMissingOrInvalidEncryptionKey(int? keyLength)
    {
        var protector = CreateProtector(keyLength == null ? null : new byte[keyLength.Value]);

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => protector.Protect("secret"));

        await Assert.That(exception.Message).Contains("encryption key").WithComparison(StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    [Arguments("")]
    [Arguments("v2.payload")]
    [Arguments("v1.payload")]
    public async Task Unprotect_RejectsUnsupportedPayloadFormat(string protectedValue)
    {
        var protector = CreateProtector("0123456789abcdef0123456789abcdef"u8.ToArray());

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => protector.Unprotect(protectedValue));

        await Assert.That(exception.Message).Contains("not supported").WithComparison(StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultSecretValueProtector CreateProtector(byte[]? key)
    {
        return new DefaultSecretValueProtector(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { EncryptionKey = key }));
    }
}
