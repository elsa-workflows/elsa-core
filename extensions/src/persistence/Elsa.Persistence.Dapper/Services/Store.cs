using System.Data;
using System.Linq.Expressions;
using Dapper;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Records;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Services;

/// <summary>
/// Provides a generic store using Dapper.
/// </summary>
[PublicAPI]
public class Store<T>(IDbConnectionProvider dbConnectionProvider, ITenantAccessor tenantAccessor, string tableName, string primaryKey = "Id")
    where T : notnull
{
    /// <summary>
    /// The name of the table.
    /// </summary>
    public string TableName { get; } = tableName;

    /// <summary>
    /// The name of the primary key column.
    /// </summary>
    public string PrimaryKey { get; } = primaryKey;

    /// <summary>
    /// Finds a single record.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The record, if found.</returns>
    public async Task<T?> FindAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        return await FindAsync(filter, false, cancellationToken);
    }

    /// <summary>
    /// Finds a single record.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The record, if found.</returns>
    public async Task<T?> FindAsync(Action<ParameterizedQuery> filter, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await FindAsync(filter, null, null, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Finds a single record using the specified order key selector and order direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The record, if found.</returns>
    public async Task<T?> FindAsync(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindAsync(filter, orderKey, orderDirection, false, cancellationToken);
    }

    /// <summary>
    /// Finds a single record using the specified order key selector and order direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The record, if found.</returns>
    public async Task<T?> FindAsync(Action<ParameterizedQuery> filter, string? orderKey = null, OrderDirection? orderDirection = null, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        ApplyTenantFilter(query, tenantAgnostic);
        filter(query);

        if (orderKey != null && orderDirection != null)
            query = query.OrderBy(orderKey, orderDirection.Value);

        using var connection = dbConnectionProvider.GetConnection();
        return await query.FirstOrDefaultAsync<T>(connection);
    }

    /// <summary>
    /// Returns a page of records.
    /// </summary>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records.</returns>
    public async Task<Page<T>> ListAsync(PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(null, pageArgs, orderKey, orderDirection, false, cancellationToken);
    }
    
    /// <summary>
    /// Returns a page of records.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records.</returns>
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery>? filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(filter, pageArgs, orderKey, orderDirection, false, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records.</returns>
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery>? filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<T>(filter, pageArgs, orderKey, orderDirection, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records.</returns>
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery>? filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(filter, pageArgs, orderFields, false, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records.</returns>
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery>? filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<T>(filter, pageArgs, orderFields, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A page of records.</returns>
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery>? filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<TShape>(filter, pageArgs, orderKey, orderDirection, false, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A page of records.</returns>
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery>? filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<TShape>(filter, pageArgs, new[]
        {
            new OrderField(orderKey, orderDirection)
        }, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A page of records.</returns>
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery>? filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<TShape>(filter, pageArgs, orderFields, false, cancellationToken);
    }

    /// <summary>
    /// Returns a page of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A page of records.</returns>
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery>? filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        ApplyTenantFilter(query, tenantAgnostic);
        filter?.Invoke(query);
        query = query.OrderBy(orderFields.ToArray()).Page(pageArgs);

        var countQuery = dbConnectionProvider.CreateQuery().Count(TableName);
        ApplyTenantFilter(countQuery, tenantAgnostic);
        filter?.Invoke(countQuery);

        using var connection = dbConnectionProvider.GetConnection();
        var records = (await query.QueryAsync<TShape>(connection)).ToList();
        var totalCount = await countQuery.SingleAsync<long>(connection);
        return Page.Of(records, totalCount);
    }

    /// <summary>
    /// Returns a set of records.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(filter, false, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<T>(filter, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<TShape>(filter, false, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        ApplyTenantFilter(query, tenantAgnostic);
        filter(query);
        return await query.QueryAsync<TShape>(connection);
    }

    /// <summary>
    /// Returns a set of records, ordered by the specified key selector in the specified direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(filter, orderKey, orderDirection, false, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records, ordered by the specified key selector in the specified direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<T>(filter, orderKey, orderDirection, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records in the specified shape, ordered by the specified key selector in the specified direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync<TShape>(filter, orderKey, orderDirection, false, cancellationToken);
    }

    /// <summary>
    /// Returns a set of records in the specified shape, ordered by the specified key selector in the specified direction.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        ApplyTenantFilter(query, tenantAgnostic);
        filter(query);
        query = query.OrderBy(orderKey, orderDirection);
        return await query.QueryAsync<TShape>(connection);
    }

    /// <summary>
    /// Adds or updates the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(T record, CancellationToken cancellationToken = default)
    {
        SetTenantId(record);
        using var connection = dbConnectionProvider.GetConnection();
        var affected = await ExecuteUpsertAsync(record, connection);
        if (affected > 0)
            return;

        await ResolveZeroRowUpsertAsync(record, connection);
    }

    /// <summary>
    /// Adds or updates the specified records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.ToList();
        if (recordsList.Count == 0)
            return;

        var query = new ParameterizedQuery(dbConnectionProvider.Dialect);
        var currentIndex = 0;

        foreach (var record in recordsList)
        {
            var index = currentIndex;
            SetTenantId(record);
            query.Upsert(TableName, PrimaryKey, record, field => $"{field}_{index}");
            currentIndex++;
        }

        using var connection = dbConnectionProvider.GetConnection();
        var affected = await query.ExecuteAsync(connection);
        // PostgreSQL and SQL Server sum the batch. SQLite's changes() is the last statement only,
        // so a successful multi-row SQLite batch often reports 1 and takes the lookup path.
        if (affected == recordsList.Count)
            return;

        await ResolveBatchTenantConflictsAsync(recordsList, connection);
    }

    /// <summary>
    /// Adds the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddAsync(T record, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        SetTenantId(record);
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Insert(TableName, record);
        await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Adds the specified records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.ToList();

        if (!recordsList.Any())
            return;

        var query = new ParameterizedQuery(dbConnectionProvider.Dialect);
        var currentIndex = 0;

        foreach (var record in recordsList)
        {
            var index = currentIndex;
            SetTenantId(record);
            query.Insert(TableName, record, field => $"{field}_{index}");
            currentIndex++;
        }

        using var connection = dbConnectionProvider.GetConnection();
        await query.ExecuteAsync(connection);
    }
    
    /// <summary>
    /// Updates the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UpdateAsync(T record, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        SetTenantId(record);
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Update(TableName, record, PrimaryKey);
        await query.ExecuteAsync(connection);
    }
    
    public async Task UpdateAsync(T record, Expression<Func<T, object>>[] props, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        SetTenantId(record);
        var fields = props.Select(x => x.GetPropertyName()).ToArray();
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Update(TableName, record, PrimaryKey, fields);
        await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Updates the specified fields on records matching the filter.
    /// </summary>
    /// <returns>The number of records updated.</returns>
    public async Task<int> UpdateAsync(T record, Expression<Func<T, object>>[] props, Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        var fields = props.Select(x => x.GetPropertyName()).ToArray();
        var query = dbConnectionProvider.CreateQuery().Update(TableName, record, fields);
        ApplyTenantFilter(query);
        filter(query);
        return await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Deletes all records matching the specified query.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    public async Task<long> DeleteAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().Delete(TableName);
        filter(query);

        // If there are no conditions, we don't want to delete all records.
        if (!query.Parameters.ParameterNames.Any())
            return 0;

        ApplyTenantFilter(query);

        using var connection = dbConnectionProvider.GetConnection();
        return await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Deletes all records matching the specified query.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="primaryKey">The primary key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    public async Task<long> DeleteAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, string primaryKey = "Id", CancellationToken cancellationToken = default)
    {
        var selectQuery = dbConnectionProvider.CreateQuery().From(TableName, primaryKey);
        filter(selectQuery);

        // If there are no conditions, we don't want to delete all records.
        if (!selectQuery.Parameters.ParameterNames.Any())
            return 0;

        ApplyTenantFilter(selectQuery, false);
        selectQuery = selectQuery.OrderBy(orderFields.ToArray()).Page(pageArgs);

        var deleteQuery = dbConnectionProvider.CreateQuery().Delete(TableName, primaryKey, selectQuery);
        using var connection = dbConnectionProvider.GetConnection();
        return await deleteQuery.ExecuteAsync(connection);
    }

    /// <summary>
    /// Returns <c>true</c> if any records match the specified query.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if any records match the specified query.</returns>
    public async Task<bool> AnyAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().From(TableName, PrimaryKey);
        ApplyTenantFilter(query, false);
        filter(query);
        using var connection = dbConnectionProvider.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<object>(query.Sql.ToString(), query.Parameters) != null;
    }

    /// <summary>
    /// Returns the number of records matching the specified query. 
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records matching the specified query.</returns>
    public async Task<long> CountAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        var countQuery = dbConnectionProvider.CreateQuery().Count(TableName);
        ApplyTenantFilter(countQuery, false);
        filter(countQuery);
        using var connection = dbConnectionProvider.GetConnection();
        return await countQuery.SingleAsync<long>(connection);
    }

    private void ApplyTenantFilter(ParameterizedQuery query, bool tenantAgnostic = false)
    {
        if (tenantAgnostic)
            return;

        var tenant = tenantAccessor.Tenant;
        var tenantId = tenant?.Id;
        // Default tenant is '' in Elsa and NULL or '' on some legacy rows (#245 / #260 / elsa-core#8618).
        if (string.IsNullOrEmpty(tenantId))
            query.IsNullOrEmpty(nameof(Record.TenantId));
        else
            query.Is(nameof(Record.TenantId), tenantId);
    }

    private void SetTenantId(T record)
    {
        if (record is not Record recordWithTenant)
            return;

        // Stamp the ambient tenant. Do not trust a caller-supplied TenantId (see #9010).
        recordWithTenant.TenantId = tenantAccessor.Tenant?.Id;
    }

    // Cross-tenant Id rule (#9010):
    // - Existing TenantId '*': same as main — no throw; ambient stamp plus a PK overwrite
    //   (the '*' row becomes the current tenant). Visibility of '*' on reads stays in #9010.
    // - Existing concrete tenant A, incoming other tenant B (a caller-supplied '*' is stamped
    //   to ambient first, so it cannot overwrite A): throw.
    // - Same tenant, treating NULL and '' as the default tenant: update.
    private async Task ResolveZeroRowUpsertAsync(T record, IDbConnection connection)
    {
        var key = GetPrimaryKeyValue(record);
        var existing = await LookupByKeyAsync(key, connection);
        if (existing is Record owned)
        {
            await ApplyExistingRowOutcomeAsync(record, owned, key, connection);
            return;
        }

        var retried = await ExecuteUpsertAsync(record, connection);
        if (retried > 0)
            return;

        throw new InvalidOperationException($"Failed to save a record with ID '{key}'.");
    }

    private async Task ResolveBatchTenantConflictsAsync(IReadOnlyList<T> records, IDbConnection connection)
    {
        var keys = records.Select(GetPrimaryKeyValue).OfType<object>().Distinct().ToList();
        var existingByKey = new Dictionary<object, T>();

        if (keys.Count > 0)
        {
            var lookup = new ParameterizedQuery(dbConnectionProvider.Dialect).From(TableName);
            lookup.In(PrimaryKey, keys);
            foreach (var existing in await lookup.QueryAsync<T>(connection))
            {
                var existingKey = GetPrimaryKeyValue(existing);
                if (existingKey != null)
                    existingByKey[existingKey] = existing;
            }
        }

        foreach (var record in records)
        {
            var key = GetPrimaryKeyValue(record);
            if (key == null || !existingByKey.TryGetValue(key, out var existing))
            {
                var retried = await ExecuteUpsertAsync(record, connection);
                if (retried > 0)
                    continue;

                throw new InvalidOperationException($"Failed to save a record with ID '{key}'.");
            }

            if (existing is Record owned)
                await ApplyExistingRowOutcomeAsync(record, owned, key, connection);
        }
    }

    private async Task ApplyExistingRowOutcomeAsync(T record, Record owned, object? key, IDbConnection connection)
    {
        if (owned.TenantId == Tenant.AgnosticTenantId)
        {
            var overwrite = new ParameterizedQuery(dbConnectionProvider.Dialect).Update(TableName, record, PrimaryKey);
            await overwrite.ExecuteAsync(connection);
            return;
        }

        var incomingTenantId = record is Record incoming ? incoming.TenantId : null;
        if (SameTenant(owned.TenantId, incomingTenantId))
            return;

        throw new InvalidOperationException($"A record with ID '{key}' already exists in another tenant.");
    }

    private async Task<int> ExecuteUpsertAsync(T record, IDbConnection connection)
    {
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Upsert(TableName, PrimaryKey, record);
        return await query.ExecuteAsync(connection);
    }

    private async Task<T?> LookupByKeyAsync(object? key, IDbConnection connection)
    {
        if (key == null)
            return default;

        var lookup = new ParameterizedQuery(dbConnectionProvider.Dialect).From(TableName);
        lookup.Is(PrimaryKey, key);
        return await lookup.FirstOrDefaultAsync<T>(connection);
    }

    private object? GetPrimaryKeyValue(T record) =>
        record.GetType().GetProperty(PrimaryKey)?.GetValue(record);

    private static bool SameTenant(string? left, string? right) =>
        string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
}