using Elsa.Common.Entities;
using Elsa.Common.Services;

namespace Elsa.Identity.Services;

/// <summary>
/// Memory counterpart of the EF <c>PerTenantIdentityUniqueness</c> indexes on
/// <c>(TenantId, Name)</c> and <c>(TenantId, ClientId)</c>.
/// </summary>
internal static class MemoryIdentityUniqueness
{
    /// <summary>
    /// Throws when another Id in the same tenant already owns <paramref name="keySelector"/>.
    /// Same-Id upserts are allowed so a row can rename itself.
    /// </summary>
    public static void EnsureAvailable<T>(
        MemoryStore<T> store,
        T entity,
        Func<T, string?> keySelector,
        string keyName)
        where T : Entity
    {
        var key = keySelector(entity);
        var existing = store.Find(candidate =>
            candidate.TenantId == entity.TenantId
            && candidate.Id != entity.Id
            && keySelector(candidate) == key);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A {typeof(T).Name.ToLowerInvariant()} already exists with {keyName} '{key}' in tenant '{entity.TenantId}'.");
        }
    }
}
