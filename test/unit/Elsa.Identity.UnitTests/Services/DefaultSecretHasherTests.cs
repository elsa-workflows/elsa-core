using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
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
        Assert.Equal(32, hashedSecret.Salt.Length);
        Assert.Equal(44, hashedSecret.EncodeSalt().Length);
        Assert.True(_hasher.VerifySecret("secret", hashedSecret, out var needsRehash));
        Assert.False(needsRehash);
    }

    [Fact]
    public void VerifySecret_ParsesPbkdf2EnvelopeUsingInvariantCulture()
    {
        var hashedSecret = CreatePbkdf2Hash("secret", 1);
        using var _ = new CultureScope("ar-SA");

        var verified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.True(verified);
        Assert.True(needsRehash);
    }

    [Fact]
    public void GenerateSalt_GeneratesExpectedSalt()
    {
        var salt = _hasher.GenerateSalt();

        Assert.Equal(32, salt.Length);
        Assert.Equal(44, Convert.ToBase64String(salt).Length);
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
    public void VerifySecret_WithCorrectPasswordAndLowerIterationCount_RequestsRehash()
    {
        var hashedSecret = CreatePbkdf2Hash("secret", 1);

        var verified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.True(verified);
        Assert.True(needsRehash);
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
    public void VerifySecret_RejectsPbkdf2HashWithInvalidKeyLength()
    {
        var salt = _hasher.GenerateSalt();
        var storedHash = Encoding.UTF8.GetBytes("pbkdf2-sha256$600000$" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
        var hashedSecret = HashedSecret.FromBytes(storedHash, salt);

        var verified = _hasher.VerifySecret("secret", hashedSecret, out var needsRehash);

        Assert.False(verified);
        Assert.False(needsRehash);
    }

    [Fact]
    public void VerifySecret_RejectsMalformedLegacyHashWithInvalidKeyLength()
    {
        var hashedSecret = HashedSecret.FromBytes(RandomNumberGenerator.GetBytes(16), _hasher.GenerateSalt());

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
        var rehashingHasher = new RehashingSecretHasher("secret");
        var validator = new DefaultUserCredentialsValidator(new StoreBasedUserProvider(userStore), userStore, rehashingHasher);

        var user = await validator.ValidateAsync("alice", "secret");
        var reloadedUser = await userStore.FindAsync(new UserFilter { Name = "alice" });

        Assert.NotNull(user);
        Assert.NotNull(reloadedUser);
        Assert.Equal(rehashingHasher.UpgradedSecret.EncodeSecret(), reloadedUser.HashedPassword);
        Assert.Equal(rehashingHasher.UpgradedSecret.EncodeSalt(), reloadedUser.HashedPasswordSalt);
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
        var rehashingHasher = new RehashingSecretHasher(apiKey);
        var validator = new DefaultApplicationCredentialsValidator(apiKeyGenerator, applicationProvider, applicationStore, rehashingHasher);

        var application = await validator.ValidateAsync(apiKey);
        var reloadedApplication = await applicationStore.FindAsync(new ApplicationFilter { ClientId = "client-1" });

        Assert.NotNull(application);
        Assert.NotNull(reloadedApplication);
        Assert.Equal(rehashingHasher.UpgradedSecret.EncodeSecret(), reloadedApplication.HashedApiKey);
        Assert.Equal(rehashingHasher.UpgradedSecret.EncodeSalt(), reloadedApplication.HashedApiKeySalt);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUserWhenLegacyPasswordRehashSaveFails()
    {
        var legacyHash = CreateLegacyHash("secret");
        var user = new User
        {
            Id = "user-1",
            Name = "alice",
            HashedPassword = legacyHash.EncodeSecret(),
            HashedPasswordSalt = legacyHash.EncodeSalt()
        };
        var userStore = new FailingUserStore(user, new TimeoutException("Save timed out."));
        var validator = new DefaultUserCredentialsValidator(new StoreBasedUserProvider(userStore), userStore, new RehashingSecretHasher("secret"));

        var validatedUser = await validator.ValidateAsync("alice", "secret");

        Assert.Same(user, validatedUser);
        Assert.Equal(legacyHash.EncodeSecret(), user.HashedPassword);
        Assert.Equal(legacyHash.EncodeSalt(), user.HashedPasswordSalt);
    }

    [Fact]
    public async Task ValidateAsync_WithOldUserValidatorConstructor_ReturnsUserWithoutPersistingRehash()
    {
        var legacyHash = CreateLegacyHash("secret");
        var encodedLegacyHash = legacyHash.EncodeSecret();
        var user = new User
        {
            Id = "user-1",
            Name = "alice",
            HashedPassword = encodedLegacyHash,
            HashedPasswordSalt = legacyHash.EncodeSalt()
        };
        var validator = new DefaultUserCredentialsValidator(new StaticUserProvider(user), new RehashingSecretHasher("secret"));

        var validatedUser = await validator.ValidateAsync("alice", "secret");

        Assert.Same(user, validatedUser);
        Assert.Equal(encodedLegacyHash, user.HashedPassword);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsApplicationWhenLegacyApiKeyRehashSaveFails()
    {
        var apiKeyGenerator = new DefaultApiKeyGeneratorAndParser();
        var apiKey = apiKeyGenerator.Generate("client-1");
        var legacyHash = CreateLegacyHash(apiKey);
        var application = new Application
        {
            Id = "app-1",
            ClientId = "client-1",
            Name = "Client 1",
            HashedApiKey = legacyHash.EncodeSecret(),
            HashedApiKeySalt = legacyHash.EncodeSalt(),
            HashedClientSecret = "",
            HashedClientSecretSalt = ""
        };
        var applicationStore = new FailingApplicationStore(application);
        var applicationProvider = new StoreBasedApplicationProvider(applicationStore);
        var validator = new DefaultApplicationCredentialsValidator(apiKeyGenerator, applicationProvider, applicationStore, new RehashingSecretHasher(apiKey));

        var validatedApplication = await validator.ValidateAsync(apiKey);

        Assert.Same(application, validatedApplication);
        Assert.Equal(legacyHash.EncodeSecret(), application.HashedApiKey);
        Assert.Equal(legacyHash.EncodeSalt(), application.HashedApiKeySalt);
    }

    [Fact]
    public async Task ValidateAsync_WithOldApplicationValidatorConstructor_ReturnsApplicationWithoutPersistingRehash()
    {
        var apiKeyGenerator = new DefaultApiKeyGeneratorAndParser();
        var apiKey = apiKeyGenerator.Generate("client-1");
        var legacyHash = CreateLegacyHash(apiKey);
        var encodedLegacyHash = legacyHash.EncodeSecret();
        var application = new Application
        {
            Id = "app-1",
            ClientId = "client-1",
            Name = "Client 1",
            HashedApiKey = encodedLegacyHash,
            HashedApiKeySalt = legacyHash.EncodeSalt(),
            HashedClientSecret = "",
            HashedClientSecretSalt = ""
        };
        var applicationProvider = new StaticApplicationProvider(application);
        var validator = new DefaultApplicationCredentialsValidator(apiKeyGenerator, applicationProvider, new RehashingSecretHasher(apiKey));

        var validatedApplication = await validator.ValidateAsync(apiKey);

        Assert.Same(application, validatedApplication);
        Assert.Equal(encodedLegacyHash, application.HashedApiKey);
    }

    [Fact]
    public async Task ValidateAsync_UsesTenantAgnosticLookupForApplications()
    {
        var apiKeyGenerator = new DefaultApiKeyGeneratorAndParser();
        var apiKey = apiKeyGenerator.Generate("client-1");
        var hasher = new DefaultSecretHasher();
        var hashedApiKey = hasher.HashSecret(apiKey);
        var application = new Application
        {
            Id = "app-1",
            ClientId = "client-1",
            Name = "Client 1",
            HashedApiKey = hashedApiKey.EncodeSecret(),
            HashedApiKeySalt = hashedApiKey.EncodeSalt(),
            HashedClientSecret = "",
            HashedClientSecretSalt = ""
        };
        var applicationProvider = new RecordingApplicationProvider(application);
        var validator = new DefaultApplicationCredentialsValidator(apiKeyGenerator, applicationProvider, hasher);

        var validatedApplication = await validator.ValidateAsync(apiKey);

        Assert.Same(application, validatedApplication);
        Assert.NotNull(applicationProvider.LastFilter);
        Assert.True(applicationProvider.LastFilter!.TenantAgnostic);
        Assert.Equal("client-1", applicationProvider.LastFilter.ClientId);
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
        var envelope = Encoding.UTF8.GetBytes($"pbkdf2-sha256${iterationCount.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(hash)}");
        return HashedSecret.FromBytes(envelope, salt);
    }

    private sealed class FailingUserStore(User user, Exception? exception = null) : IUserStore
    {
        private readonly User _user = user;
        private readonly Exception _exception = exception ?? new InvalidOperationException("Save failed.");

        public Task SaveAsync(User user, CancellationToken cancellationToken = default) => throw _exception;

        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var users = filter.Apply(new[] { _user }.AsQueryable()).ToList();
            return Task.FromResult<IEnumerable<User>>(users);
        }

        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = filter.Apply(new[] { _user }.AsQueryable()).FirstOrDefault();
            return Task.FromResult(user);
        }
    }

    private sealed class RehashingSecretHasher(string expectedSecret) : ISecretHasher
    {
        public HashedSecret UpgradedSecret { get; } = HashedSecret.FromBytes(Encoding.UTF8.GetBytes("upgraded-secret"), Encoding.UTF8.GetBytes("upgraded-salt"));

        public HashedSecret HashSecret(string secret) => UpgradedSecret;

        public HashedSecret HashSecret(string secret, byte[] salt) => UpgradedSecret;

        public bool VerifySecret(string clearTextSecret, string secret, string salt) => clearTextSecret == expectedSecret;

        public bool VerifySecret(string clearTextSecret, string secret, string salt, out bool needsRehash)
        {
            needsRehash = clearTextSecret == expectedSecret;
            return clearTextSecret == expectedSecret;
        }

        public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret) => clearTextSecret == expectedSecret;

        public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret, out bool needsRehash)
        {
            needsRehash = clearTextSecret == expectedSecret;
            return clearTextSecret == expectedSecret;
        }

        public byte[] HashSecret(byte[] secret, byte[] salt) => UpgradedSecret.Secret;

        public byte[] GenerateSalt(int saltSize = 32) => UpgradedSecret.Salt;
    }

    private sealed class FailingApplicationStore(Application application) : IApplicationStore
    {
        private readonly Application _application = application;

        public Task SaveAsync(Application application, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Save failed.");

        public Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
        {
            var application = filter.Apply(new[] { _application }.AsQueryable()).FirstOrDefault();
            return Task.FromResult(application);
        }
    }

    private sealed class StaticUserProvider(User user) : IUserProvider
    {
        private readonly User _user = user;

        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = filter.Apply(new[] { _user }.AsQueryable()).FirstOrDefault();
            return Task.FromResult(user);
        }
    }

    private sealed class StaticApplicationProvider(Application application) : IApplicationProvider
    {
        private readonly Application _application = application;

        public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
        {
            var application = filter.Apply(new[] { _application }.AsQueryable()).FirstOrDefault();
            return Task.FromResult(application);
        }
    }

    private sealed class RecordingApplicationProvider(Application application) : IApplicationProvider
    {
        private readonly Application _application = application;
        public ApplicationFilter? LastFilter { get; private set; }

        public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            var application = filter.Apply(new[] { _application }.AsQueryable()).FirstOrDefault();
            return Task.FromResult(application);
        }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _currentCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _currentUICulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentUICulture;
        }
    }
}
