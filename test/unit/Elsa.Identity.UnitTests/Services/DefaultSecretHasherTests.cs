using System.Security.Cryptography;
using System.Text;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultSecretHasherTests
{
    private readonly DefaultSecretHasher _hasher = new();

    [Fact]
    public void HashSecret_GeneratesVersionedPbkdf2Hash()
    {
        var hashedSecret = _hasher.HashSecret("secret");

        Assert.StartsWith("pbkdf2-sha256$600000$", Encoding.UTF8.GetString(hashedSecret.Secret));
        Assert.True(_hasher.VerifySecret("secret", hashedSecret, out var needsRehash));
        Assert.False(needsRehash);
    }

    [Fact]
    public void HashSecret_UsesUniqueSalts()
    {
        var first = _hasher.HashSecret("secret");
        var second = _hasher.HashSecret("secret");

        Assert.NotEqual(first.EncodeSalt(), second.EncodeSalt());
        Assert.NotEqual(first.EncodeSecret(), second.EncodeSecret());
    }

    [Fact]
    public void VerifySecret_AcceptsLegacySha256HashAndRequestsRehash()
    {
        var hashedSecret = CreateLegacyHash("secret");

        var verified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.True(verified);
        Assert.True(needsRehash);
    }

    [Fact]
    public void VerifySecret_WithWrongPasswordAndLowerIterationCount_DoesNotRequestRehash()
    {
        var hashedSecret = CreatePbkdf2Hash("secret", 1);

        var verified = _hasher.VerifySecret("wrong-secret", hashedSecret, out var needsRehash);

        Assert.False(verified);
        Assert.False(needsRehash);
    }

    [Fact]
    public void VerifySecret_RejectsPbkdf2HashWithExcessiveIterations()
    {
        var salt = _hasher.GenerateSalt();
        var storedHash = Encoding.UTF8.GetBytes("pbkdf2-sha256$999999999$" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var hashedSecret = HashedSecret.FromBytes(storedHash, salt);

        var verified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.False(verified);
        Assert.False(needsRehash);
    }

    [Fact]
    public async Task ValidateAsync_RehashesLegacyUserPassword()
    {
        var userStore = new MemoryUserStore(new MemoryStore<User>());
        var legacyHash = CreateLegacyHash("secret");
        await userStore.SaveAsync(new User
        {
            Id = "user-1",
            Name = "alice",
            HashedPassword = legacyHash.EncodeSecret(),
            HashedPasswordSalt = legacyHash.EncodeSalt()
        });
        var validator = new DefaultUserCredentialsValidator(new StoreBasedUserProvider(userStore), userStore, _hasher);

        var user = await validator.ValidateAsync("alice", "secret");
        var reloadedUser = await userStore.FindAsync(new UserFilter { Name = "alice" });

        Assert.NotNull(user);
        Assert.NotNull(reloadedUser);
        Assert.StartsWith("pbkdf2-sha256$", Encoding.UTF8.GetString(Convert.FromBase64String(reloadedUser.HashedPassword)));
    }

    [Fact]
    public async Task ValidateAsync_RehashesLegacyApplicationApiKey()
    {
        var apiKeyGenerator = new DefaultApiKeyGeneratorAndParser();
        var apiKey = apiKeyGenerator.Generate("client-1");
        var applicationStore = new MemoryApplicationStore(new MemoryStore<Application>());
        var legacyHash = CreateLegacyHash(apiKey);
        await applicationStore.SaveAsync(new Application
        {
            Id = "app-1",
            ClientId = "client-1",
            Name = "Client 1",
            HashedApiKey = legacyHash.EncodeSecret(),
            HashedApiKeySalt = legacyHash.EncodeSalt(),
            HashedClientSecret = "",
            HashedClientSecretSalt = ""
        });
        var applicationProvider = new StoreBasedApplicationProvider(applicationStore);
        var validator = new DefaultApplicationCredentialsValidator(apiKeyGenerator, applicationProvider, applicationStore, _hasher);

        var application = await validator.ValidateAsync(apiKey);
        var reloadedApplication = await applicationStore.FindAsync(new ApplicationFilter { ClientId = "client-1" });

        Assert.NotNull(application);
        Assert.NotNull(reloadedApplication);
        Assert.StartsWith("pbkdf2-sha256$", Encoding.UTF8.GetString(Convert.FromBase64String(reloadedApplication.HashedApiKey)));
    }

    private static HashedSecret CreateLegacyHash(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(secretBytes.Concat(salt).ToArray());
        return HashedSecret.FromBytes(hash, salt);
    }

    private static HashedSecret CreatePbkdf2Hash(string secret, int iterationCount)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secretBytes, salt, iterationCount, HashAlgorithmName.SHA256, 32);
        var envelope = Encoding.UTF8.GetBytes($"pbkdf2-sha256${iterationCount}${Convert.ToBase64String(hash)}");
        return HashedSecret.FromBytes(envelope, salt);
    }
}
