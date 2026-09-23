using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Services;
using Elsa.Secrets.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ElsaSecret = Elsa.Secrets.Models.Secret;

const string applicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
const string purpose = "Elsa.Secrets.Encryption";
var coreKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
var wrongCoreKey = Enumerable.Range(33, 32).Select(value => (byte)value).ToArray();

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: CoreCryptoRunner <old-key-ring-dir> <wrong-key-ring-dir> <missing-key-ring-dir> <ciphertext-path>");
    return 64;
}

var legacyCiphertexts = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(args[3]))
    ?? throw new InvalidOperationException("The synthetic legacy ciphertext fixture is missing.");
if (legacyCiphertexts.Length != 2)
    throw new InvalidOperationException("The bridge proof expects exactly two synthetic legacy versions.");

var wrongLegacyKeyRingRejected = legacyCiphertexts.All(ciphertext =>
    !CanUnprotect(args[1], ciphertext, applicationName, purpose));
var missingLegacyKeyRingRejected = legacyCiphertexts.All(ciphertext =>
    !CanUnprotect(args[2], ciphertext, applicationName, purpose));

using var oldServices = CreateDataProtectionServices(args[0], applicationName);
var oldDataProtector = oldServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(purpose);
var oldPlaintexts = legacyCiphertexts.Select(oldDataProtector.Unprotect).ToArray();
var sourcePlaintextHashes = oldPlaintexts.Select(Hash).ToArray();

var coreProtector = new DefaultSecretValueProtector(Options.Create(new SecretsOptions { EncryptionKey = coreKey }));
var encryptedStore = new EncryptedSecretStore(coreProtector);
var now = DateTimeOffset.UtcNow;
var secret = new ElsaSecret
{
    Id = "synthetic-secret-id",
    Name = "synthetic-secret",
    DisplayName = "synthetic-secret",
    Status = SecretStatus.Active,
    CreatedAt = now.AddDays(-1),
    UpdatedAt = now,
    Versions =
    [
        new SecretVersion
        {
            Version = 1,
            Status = SecretStatus.Retired,
            CreatedAt = now.AddDays(-1),
            ExpiresAt = now.AddHours(-1),
            Payload = SecretPayload.FromValue(oldPlaintexts[0])
        },
        new SecretVersion
        {
            Version = 2,
            Status = SecretStatus.Active,
            CreatedAt = now.AddHours(-1),
            ExpiresAt = now.AddHours(1),
            Payload = SecretPayload.FromValue(oldPlaintexts[1])
        }
    ]
};

var expectedLegacyMetadata = new Dictionary<int, Dictionary<string, string>>
{
    [1] = new()
    {
        ["legacy.rowId"] = "synthetic-row-version-1",
        ["legacy.secretId"] = secret.Id,
        ["legacy.name"] = "synthetic-secret",
        ["legacy.description"] = "previous version",
        ["legacy.scope"] = "credential",
        ["legacy.expiresIn"] = "00:15:00",
        ["legacy.isLatest"] = "false",
        ["legacy.statusValue"] = "1",
        ["legacy.updatedAt"] = now.AddDays(-1).ToString("O")
    },
    [2] = new()
    {
        ["legacy.rowId"] = "synthetic-row-version-2",
        ["legacy.secretId"] = secret.Id,
        ["legacy.name"] = "synthetic-secret",
        ["legacy.description"] = "current version",
        ["legacy.scope"] = "credential",
        ["legacy.expiresIn"] = "01:00:00",
        ["legacy.isLatest"] = "true",
        ["legacy.statusValue"] = "0",
        ["legacy.updatedAt"] = now.ToString("O"),
        ["legacy.lastAccessedAt"] = now.AddMinutes(-30).ToString("O")
    }
};

for (var index = 0; index < secret.Versions.Count; index++)
{
    var version = secret.Versions[index];
    foreach (var (key, value) in expectedLegacyMetadata[version.Version])
        version.Payload.Metadata[key] = value;
    version.Payload = await encryptedStore.WriteAsync(secret, version, version.Payload);
}

var readBacks = new List<string>();
var rawLegacyCiphertextCopyRejected = true;
foreach (var (version, index) in secret.Versions.Select((version, index) => (version, index)))
{
    var readBack = await encryptedStore.ReadAsync(secret, version);
    readBacks.Add(readBack?.Value ?? string.Empty);
    var legacyVersion = new SecretVersion
    {
        Version = version.Version,
        Payload = new SecretPayload
        {
            Metadata = new Dictionary<string, string> { ["protectedValue"] = legacyCiphertexts[index] }
        }
    };
    try
    {
        _ = await encryptedStore.ReadAsync(secret, legacyVersion);
        rawLegacyCiphertextCopyRejected = false;
    }
    catch (InvalidOperationException)
    {
        // The Core AES-GCM store rejects the old Data Protection envelope.
    }
}

var coreReadBackHashes = readBacks.Select(Hash).ToArray();
var roundTripPassed = readBacks.SequenceEqual(oldPlaintexts)
    && sourcePlaintextHashes.SequenceEqual(coreReadBackHashes);
var wrongStore = new EncryptedSecretStore(new DefaultSecretValueProtector(
    Options.Create(new SecretsOptions { EncryptionKey = wrongCoreKey })));
var wrongCoreEncryptionKeyRejected = await RejectsCoreRead(wrongStore, secret);
var missingCoreEncryptionKeyRejected = false;
try
{
    _ = new DefaultSecretValueProtector(Options.Create(new SecretsOptions())).Protect(oldPlaintexts[0]);
}
catch (InvalidOperationException)
{
    missingCoreEncryptionKeyRejected = true;
}

var metadataPreserved = secret.Versions.All(version =>
{
    var expected = expectedLegacyMetadata[version.Version];
    var actual = version.Payload.Metadata;
    var actualLegacyKeys = actual.Keys
        .Where(key => !string.Equals(key, "protectedValue", StringComparison.OrdinalIgnoreCase))
        .OrderBy(key => key, StringComparer.Ordinal)
        .ToArray();
    var expectedKeys = expected.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();

    return expected.Count == actualLegacyKeys.Length
        && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && value == pair.Value)
        && actualLegacyKeys.SequenceEqual(expectedKeys)
        && actual.TryGetValue("protectedValue", out var protectedValue)
        && !string.IsNullOrWhiteSpace(protectedValue);
});
var latestActiveVersion = secret.LatestActiveVersion?.Version;
var previousVersionExpired = secret.Versions[0].IsExpired();

Console.WriteLine(JsonSerializer.Serialize(new
{
    phase = "core-3.8.4",
    result = "verified",
    dataProtectionPurpose = purpose,
    applicationName,
    syntheticVersions = secret.Versions.Count,
    sourcePlaintextSha256 = sourcePlaintextHashes,
    coreReadBackSha256 = coreReadBackHashes,
    roundTripPassed,
    rawLegacyCiphertextCopyRejected,
    wrongLegacyKeyRingRejected,
    missingLegacyKeyRingRejected,
    wrongCoreEncryptionKeyRejected,
    missingCoreEncryptionKeyRejected,
    corePayloadHasProtectedValue = secret.Versions.All(version => version.Payload.Metadata.ContainsKey("protectedValue")),
    legacyVersionMetadataPreserved = metadataPreserved,
    latestActiveVersion,
    previousVersionExpired
}));

return roundTripPassed && rawLegacyCiphertextCopyRejected && wrongLegacyKeyRingRejected
    && missingLegacyKeyRingRejected && wrongCoreEncryptionKeyRejected && missingCoreEncryptionKeyRejected
    && metadataPreserved && latestActiveVersion == 2 && previousVersionExpired ? 0 : 1;

static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

static bool CanUnprotect(string keyRingDirectory, string ciphertext, string applicationName, string purpose)
{
    using var services = CreateDataProtectionServices(keyRingDirectory, applicationName);
    try
    {
        _ = services.GetRequiredService<IDataProtectionProvider>().CreateProtector(purpose).Unprotect(ciphertext);
        return true;
    }
    catch (CryptographicException)
    {
        return false;
    }
}

static async Task<bool> RejectsCoreRead(EncryptedSecretStore store, ElsaSecret secret)
{
    foreach (var version in secret.Versions)
    {
        try
        {
            _ = await store.ReadAsync(secret, version);
            return false;
        }
        catch (CryptographicException)
        {
            // The wrong Core key is expected to fail authentication.
        }
    }

    return true;
}

static ServiceProvider CreateDataProtectionServices(string keyRingDirectory, string applicationName)
{
    var services = new ServiceCollection();
    services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory))
        .SetApplicationName(applicationName);
    return services.BuildServiceProvider();
}
