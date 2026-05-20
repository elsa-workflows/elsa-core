using System.Text;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.Identity.UnitTests.Services;

public class ISecretHasherTests
{
    private readonly ISecretHasher _hasher = new BackwardCompatibleSecretHasher();

    [Fact]
    public void VerifySecret_WithStringSaltAndNeedsRehash_DelegatesToExistingImplementation()
    {
        var isVerified = _hasher.VerifySecret("secret", "secret", "salt", out var needsRehash);

        Assert.True(isVerified);
        Assert.False(needsRehash);
    }

    [Fact]
    public void VerifySecret_WithHashedSecretAndNeedsRehash_DelegatesToExistingImplementation()
    {
        var hashedSecret = HashedSecret.FromBytes(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes("salt"));

        var isVerified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.True(isVerified);
        Assert.False(needsRehash);
    }

    private sealed class BackwardCompatibleSecretHasher : ISecretHasher
    {
        public HashedSecret HashSecret(string secret) => throw new NotSupportedException();

        public HashedSecret HashSecret(string secret, byte[] salt) => throw new NotSupportedException();

        public byte[] HashSecret(byte[] secret, byte[] salt) => throw new NotSupportedException();

        public bool VerifySecret(string clearTextSecret, string secret, string salt)
        {
            return clearTextSecret == secret && salt == "salt";
        }

        public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret)
        {
            return clearTextSecret == Encoding.UTF8.GetString(hashedSecret.Secret);
        }
    }
}
