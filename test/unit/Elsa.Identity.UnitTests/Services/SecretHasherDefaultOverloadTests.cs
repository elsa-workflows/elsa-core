using System.Text;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class SecretHasherDefaultOverloadTests
{
    private readonly ISecretHasher _hasher = new BackwardCompatibleSecretHasher();

    [Test]
    public async Task VerifySecret_WithStringSalt_SetsNeedsRehashFalseWhenUsingDefaultInterfaceOverload()
    {
        var isVerified = _hasher.VerifySecret("secret", "secret", "salt", out var needsRehash);

        await Assert.That(isVerified).IsTrue();
        await Assert.That(needsRehash).IsFalse();
    }

    [Test]
    public async Task VerifySecret_WithHashedSecret_SetsNeedsRehashFalseWhenUsingDefaultInterfaceOverload()
    {
        var hashedSecret = HashedSecret.FromBytes(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes("salt"));

        var isVerified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        await Assert.That(isVerified).IsTrue();
        await Assert.That(needsRehash).IsFalse();
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