using Elsa.Secrets.Options;
using Elsa.Secrets.Services;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretValueProtectorTests
{
    [Fact]
    public void Protect_AndUnprotect_RoundTripsValue()
    {
        var protector = CreateProtector("0123456789abcdef0123456789abcdef"u8.ToArray());

        var protectedValue = protector.Protect("plain-text-secret");
        var value = protector.Unprotect(protectedValue);

        Assert.NotEqual("plain-text-secret", protectedValue);
        Assert.Equal("plain-text-secret", value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void Protect_RejectsMissingOrInvalidEncryptionKey(int? keyLength)
    {
        var protector = CreateProtector(keyLength == null ? null : new byte[keyLength.Value]);

        var exception = Assert.Throws<InvalidOperationException>(() => protector.Protect("secret"));

        Assert.Contains("encryption key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("v2.payload")]
    [InlineData("v1.payload")]
    public void Unprotect_RejectsUnsupportedPayloadFormat(string protectedValue)
    {
        var protector = CreateProtector("0123456789abcdef0123456789abcdef"u8.ToArray());

        var exception = Assert.Throws<InvalidOperationException>(() => protector.Unprotect(protectedValue));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultSecretValueProtector CreateProtector(byte[]? key)
    {
        return new DefaultSecretValueProtector(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { EncryptionKey = key }));
    }
}
