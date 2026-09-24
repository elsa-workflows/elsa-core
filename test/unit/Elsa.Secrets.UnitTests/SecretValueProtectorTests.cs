using System.Security.Cryptography;
using System.Text;
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

    [Fact]
    public void Unprotect_DecryptsNistAesGcmKnownAnswerVector()
    {
        // NIST GCM Test Case 2: https://csrc.nist.gov/Projects/Cryptographic-Algorithm-Validation-Program/CAVP-TESTING-BLOCK-CIPHER-MODES
        var key = new byte[16];
        var nonce = new byte[12];
        var tag = Convert.FromHexString("ab6e47d42cec13bdf53a67b21257bddf");
        var ciphertext = Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78");
        var payload = string.Join('.',
            "v1",
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));
        var protector = CreateProtector(key);

        var plaintext = protector.Unprotect(payload);

        Assert.Equal(new string('\0', 16), plaintext);
    }

    [Fact]
    public void Protect_ProducesPayloadDecryptableByIndependentAesGcm()
    {
        var key = "0123456789abcdef"u8.ToArray();
        const string expectedPlaintext = "synthetic-known-key-check";
        var protector = CreateProtector(key);

        var parts = protector.Protect(expectedPlaintext).Split('.');

        Assert.Equal("v1", parts[0]);
        Assert.Equal(4, parts.Length);
        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        Assert.Equal(expectedPlaintext, Encoding.UTF8.GetString(plaintext));
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
