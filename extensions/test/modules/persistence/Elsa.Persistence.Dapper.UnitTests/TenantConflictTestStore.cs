using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Services;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Test subclass that records write-path interleavings through <c>internal virtual</c> seams on <see cref="Store{T}"/>.
/// </summary>
internal sealed class TenantConflictTestStore<T>(
    IDbConnectionProvider dbConnectionProvider,
    ITenantAccessor tenantAccessor,
    string tableName,
    string primaryKey = "Id")
    : Store<T>(dbConnectionProvider, tenantAccessor, tableName, primaryKey)
    where T : notnull
{
    public int? ForcedAffectedRows { get; set; }

    public Action<T, T?>? AfterExistingRowLookup { get; set; }

    internal override int? ConsumeForcedAffectedRows()
    {
        var forced = ForcedAffectedRows;
        ForcedAffectedRows = null;
        return forced;
    }

    internal override void OnAfterExistingRowLookup(T record, T? existing) =>
        AfterExistingRowLookup?.Invoke(record, existing);
}
