using System.Security.Cryptography;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using Microsoft.Extensions.Hosting;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Secrets.UnitTests;

public class SecretKeyRotationTests
{
    private static readonly byte[] OldKey = SHA256.HashData("synthetic-old"u8);
    private static readonly byte[] NewKey = SHA256.HashData("synthetic-new"u8);

    [Fact]
    public void ProductionRejectsMissingAndKnownSampleKeys()
    {
        var environment = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production }).Environment;
        var validator = new ValidateSecretsOptions(environment);

        Assert.True(validator.Validate(null, new SecretsOptions()).Failed);
        Assert.True(validator.Validate(null, new SecretsOptions { EncryptionKey = "CHANGE_ME_TO_A_SECURE_RANDOM_KEY"u8.ToArray() }).Failed);
        Assert.True(validator.Validate(null, new SecretsOptions { EncryptionKey = NewKey }).Succeeded);
    }

    [Fact]
    public async Task ReencryptionPreservesRetainedVersionsAndCanBeSafelyRetried()
    {
        var repository = new InMemorySecretRepository();
        var old = new DefaultSecretValueProtector(MicrosoftOptions.Create(new SecretsOptions { EncryptionKey = OldKey }));
        var current = new DefaultSecretValueProtector(MicrosoftOptions.Create(new SecretsOptions { EncryptionKey = NewKey }));
        var keyRing = MicrosoftOptions.Create(new SecretsOptions { EncryptionKey = NewKey, PreviousEncryptionKeys = [OldKey] });
        var ciphertext = old.Protect("archived marker");
        var secret = CreateSecret(ciphertext);
        await repository.AddAsync(secret);
        var rotation = new SecretKeyRotation(repository, keyRing);

        Assert.Equal("archived marker", new DefaultSecretValueProtector(keyRing).Unprotect(ciphertext));
        Assert.Equal(1, await rotation.ReencryptAsync());
        Assert.Equal(ciphertext, (await repository.GetAsync("archive"))!.Versions[0].Payload.Metadata["protectedValue"]);

        Assert.Equal(1, await rotation.ReencryptAsync(dryRun: false));

        var version = Assert.Single((await repository.GetAsync("archive"))!.Versions);
        Assert.Equal(4, version.Version);
        Assert.Equal(SecretStatus.Revoked, version.Status);
        Assert.Equal("retain", version.Payload.Metadata["label"]);
        Assert.Equal("archived marker", current.Unprotect(version.Payload.Metadata["protectedValue"]));
        Assert.Equal(0, await rotation.ReencryptAsync());
    }

    [Fact]
    public async Task AFailedPreflightDoesNotRewriteGoodCiphertext()
    {
        var repository = new InMemorySecretRepository();
        var old = new DefaultSecretValueProtector(MicrosoftOptions.Create(new SecretsOptions { EncryptionKey = OldKey }));
        var keyRing = MicrosoftOptions.Create(new SecretsOptions { EncryptionKey = NewKey, PreviousEncryptionKeys = [OldKey] });
        var ciphertext = old.Protect("archived marker");
        await repository.AddAsync(CreateSecret(ciphertext));
        var broken = CreateSecret("bad-envelope");
        broken.Name = "broken";
        await repository.AddAsync(broken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new SecretKeyRotation(repository, keyRing).ReencryptAsync(dryRun: false));

        Assert.Equal(ciphertext, (await repository.GetAsync("archive"))!.Versions[0].Payload.Metadata["protectedValue"]);
    }

    private static Secret CreateSecret(string ciphertext) => new()
    {
        Name = "archive",
        DisplayName = "Archive",
        Versions =
        [
            new SecretVersion
            {
                Version = 4,
                Status = SecretStatus.Revoked,
                Payload = new SecretPayload { Metadata = new Dictionary<string, string> { ["protectedValue"] = ciphertext, ["label"] = "retain" } }
            }
        ]
    };
}
