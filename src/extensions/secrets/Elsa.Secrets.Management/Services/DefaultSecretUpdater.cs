using Elsa.Common;
using Elsa.Workflows;

namespace Elsa.Secrets.Management;

public class DefaultSecretUpdater(ISecretStore store, IEncryptor encryptor, IIdentityGenerator identityGenerator, ISystemClock systemClock) : ISecretUpdater
{
    public async Task<Secret> UpdateAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var secretId = secret.SecretId;
        var filter = new SecretFilter
        {
            SecretId = secretId,
            IsLatest = true
        };
        
        // There should always be at most one latest version of the secret, but we'll use FindManyAsync to be safe as a defensive programming measure.
        var currentLatestVersions = (await store.FindManyAsync(filter, cancellationToken)).OrderBy(x => x.Version).ToList();
        var currentLatestVersion = currentLatestVersions.LastOrDefault() ?? secret;

        foreach (var version in currentLatestVersions)
        {
            version.IsLatest = false;
            
            // Only retire the version if it's active.
            if(version.Status == SecretStatus.Active)
                version.Status = SecretStatus.Retired;
            await store.UpdateAsync(version, cancellationToken);
        }
        
        var newVersion = secret.Clone();
        var encryptedValue = await encryptor.EncryptAsync(input.Value, cancellationToken);
        newVersion.Id = identityGenerator.GenerateId();
        newVersion.IsLatest = true;
        newVersion.Version = currentLatestVersion.Version + 1;
        newVersion.Name = input.Name.Trim();
        newVersion.Scope = input.Scope?.Trim();
        newVersion.Description = input.Description.Trim();
        newVersion.EncryptedValue = encryptedValue;
        newVersion.ExpiresIn = input.ExpiresIn;
        newVersion.ExpiresAt = input.ExpiresIn != null ? systemClock.UtcNow + input.ExpiresIn.Value : null;
        newVersion.Status = SecretStatus.Active;
        
        await store.AddAsync(newVersion, cancellationToken);
        return newVersion;
    }
}