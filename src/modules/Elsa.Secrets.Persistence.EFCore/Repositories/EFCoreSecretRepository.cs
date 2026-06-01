using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.Repositories;

public class EFCoreSecretRepository(Store<SecretsElsaDbContext, Secret> store) : ISecretRepository
{
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
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var normalizedName = NormalizeName(secret.Name);
        if (await ExistsByNormalizedNameAsync(dbContext, normalizedName, cancellationToken))
            throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

        await dbContext.Secrets.AddAsync(secret, cancellationToken);
        SetNormalizedName(dbContext, secret);
        SecretSerialization.StoreSerializedProperties(dbContext, secret);
        await SaveChangesAsync(dbContext, secret.Name, cancellationToken);
    }

    public async Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var existingSecret = await FindByNameAsync(dbContext, secret.Name, cancellationToken);

        if (existingSecret == null)
        {
            await dbContext.Secrets.AddAsync(secret, cancellationToken);
            SetNormalizedName(dbContext, secret);
            SecretSerialization.StoreSerializedProperties(dbContext, secret);
            return await TrySaveChangesAsync(dbContext, secret.Name, cancellationToken);
        }

        if (existingSecret.Status != SecretStatus.Deleted)
            return false;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Secrets.Remove(existingSecret);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Secrets.AddAsync(secret, cancellationToken);
        SetNormalizedName(dbContext, secret);
        SecretSerialization.StoreSerializedProperties(dbContext, secret);

        if (!await TrySaveChangesAsync(dbContext, secret.Name, cancellationToken))
            return false;

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var existingSecret = await FindByNameAsync(dbContext, secret.Name, cancellationToken);

        if (existingSecret == null)
        {
            await dbContext.Secrets.AddAsync(secret, cancellationToken);
            SetNormalizedName(dbContext, secret);
            SecretSerialization.StoreSerializedProperties(dbContext, secret);
        }
        else
        {
            Copy(secret, existingSecret);
            SetNormalizedName(dbContext, existingSecret);
            SecretSerialization.StoreSerializedProperties(dbContext, existingSecret);
        }

        await SaveChangesAsync(dbContext, secret.Name, cancellationToken);
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
        return dbContext.Secrets.FirstOrDefaultAsync(x => EF.Property<string>(x, SecretShadowPropertyNames.NormalizedName) == normalizedName, cancellationToken);
    }

    private static Task<bool> ExistsByNormalizedNameAsync(SecretsElsaDbContext dbContext, string normalizedName, CancellationToken cancellationToken)
    {
        return dbContext.Secrets.AnyAsync(x => EF.Property<string>(x, SecretShadowPropertyNames.NormalizedName) == normalizedName, cancellationToken);
    }

    private async Task SaveChangesAsync(SecretsElsaDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e)
        {
            if (await IsNameConflictAsync(name, cancellationToken))
                throw new InvalidOperationException($"A secret named '{name}' already exists.", e);

            throw;
        }
    }

    private async Task<bool> TrySaveChangesAsync(SecretsElsaDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            if (await IsNameConflictAsync(name, cancellationToken))
                return false;

            throw;
        }
    }

    private async Task<bool> IsNameConflictAsync(string name, CancellationToken cancellationToken)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        return await ExistsByNormalizedNameAsync(dbContext, NormalizeName(name), cancellationToken);
    }

    private static void SetNormalizedName(SecretsElsaDbContext dbContext, Secret secret)
    {
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.NormalizedName).CurrentValue = NormalizeName(secret.Name);
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
