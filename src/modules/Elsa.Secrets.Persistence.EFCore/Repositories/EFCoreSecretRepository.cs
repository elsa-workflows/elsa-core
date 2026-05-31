using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.Repositories;

public class EFCoreSecretRepository(Store<SecretsElsaDbContext, Secret> store) : ISecretRepository
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var secret = await FindByNameAsync(dbContext, normalizedName, cancellationToken);
        SecretSerialization.LoadSerializedProperties(dbContext, secret);
        return secret;
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var secrets = await dbContext.Secrets.ToListAsync(cancellationToken);

        foreach (var secret in secrets)
            SecretSerialization.LoadSerializedProperties(dbContext, secret);

        return secrets;
    }

    public async Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
            var normalizedName = NormalizeName(secret.Name);
            if (await dbContext.Secrets.AnyAsync(x => x.Name.ToLower() == normalizedName, cancellationToken))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            await dbContext.Secrets.AddAsync(secret, cancellationToken);
            SecretSerialization.StoreSerializedProperties(dbContext, secret);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
            var existingSecret = await FindByNameAsync(dbContext, secret.Name, cancellationToken);

            if (existingSecret == null)
            {
                await dbContext.Secrets.AddAsync(secret, cancellationToken);
                SecretSerialization.StoreSerializedProperties(dbContext, secret);
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            if (existingSecret.Status != SecretStatus.Deleted)
                return false;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Secrets.Remove(existingSecret);
            await dbContext.SaveChangesAsync(cancellationToken);
            await dbContext.Secrets.AddAsync(secret, cancellationToken);
            SecretSerialization.StoreSerializedProperties(dbContext, secret);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
            var existingSecret = await FindByNameAsync(dbContext, secret.Name, cancellationToken);

            if (existingSecret == null)
            {
                await dbContext.Secrets.AddAsync(secret, cancellationToken);
                SecretSerialization.StoreSerializedProperties(dbContext, secret);
            }
            else
            {
                Copy(secret, existingSecret);
                SecretSerialization.StoreSerializedProperties(dbContext, existingSecret);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static void Copy(Secret source, Secret target)
    {
        target.Name = source.Name;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.TypeName = source.TypeName;
        target.StoreName = source.StoreName;
        target.Scope = source.Scope;
        target.Status = source.Status;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.Tags = source.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        target.Versions = source.Versions.ToList();
    }

    private static Task<Secret?> FindByNameAsync(SecretsElsaDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        return dbContext.Secrets.FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName, cancellationToken);
    }

    private static string NormalizeName(string name) => name.ToLowerInvariant();
}
