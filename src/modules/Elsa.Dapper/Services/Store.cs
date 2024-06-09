using Dapper;
using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Records;
using Elsa.Framework.Entities;
using Elsa.Framework.Tenants.Contracts;
using Elsa.Tenants;
using JetBrains.Annotations;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a generic store using Dapper.
/// </summary>
[PublicAPI]
public class Store<T>(IDbConnectionProvider dbConnectionProvider, ITenantResolver tenantResolver, string tableName, string primaryKey = "Id")
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
        await ApplyTenantFilterAsync(query, tenantAgnostic, cancellationToken);
        filter(query);

        if (orderKey != null && orderDirection != null)
            query = query.OrderBy(orderKey, orderDirection.Value);

        using var connection = dbConnectionProvider.GetConnection();
        return await query.FirstOrDefaultAsync<T>(connection);
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
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
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
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, bool tenantAgnostic, CancellationToken cancellationToken = default)
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
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, CancellationToken cancellationToken = default)
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
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, bool tenantAgnostic, CancellationToken cancellationToken = default)
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
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default)
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
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, bool tenantAgnostic, CancellationToken cancellationToken = default)
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
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, CancellationToken cancellationToken = default)
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
    public async Task<Page<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        await ApplyTenantFilterAsync(query, tenantAgnostic, cancellationToken);
        filter(query);
        query = query.OrderBy(orderFields.ToArray()).Page(pageArgs);

        var countQuery = dbConnectionProvider.CreateQuery().Count(TableName);
        filter(countQuery);

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
        await ApplyTenantFilterAsync(query, tenantAgnostic, cancellationToken);
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
        await ApplyTenantFilterAsync(query, tenantAgnostic, cancellationToken);
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
        using var connection = dbConnectionProvider.GetConnection();
        await SetTenantIdAsync(record, cancellationToken);
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Upsert(TableName, PrimaryKey, record);
        await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Adds or updates the specified records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.ToList();

        if (!recordsList.Any())
            return;

        var query = new ParameterizedQuery(dbConnectionProvider.Dialect);
        var currentIndex = 0;

        foreach (var record in recordsList)
        {
            var index = currentIndex;
            await SetTenantIdAsync(record, cancellationToken);
            query.Upsert(TableName, PrimaryKey, record, field => $"{field}_{index}");
            currentIndex++;
        }

        using var connection = dbConnectionProvider.GetConnection();
        await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Adds the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddAsync(T record, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        await SetTenantIdAsync(record, cancellationToken);
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
        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Insert(TableName, record);
        await query.ExecuteAsync(connection);
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

        // If there are no conditions, we don't want to delete all records.
        if (!query.Parameters.ParameterNames.Any())
            return 0;

        await ApplyTenantFilterAsync(query, false, cancellationToken);
        filter(query);

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

        // If there are no conditions, we don't want to delete all records.
        if (!selectQuery.Parameters.ParameterNames.Any())
            return 0;

        await ApplyTenantFilterAsync(selectQuery, false, cancellationToken);
        filter(selectQuery);
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
        await ApplyTenantFilterAsync(query, false, cancellationToken);
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
        await ApplyTenantFilterAsync(countQuery, false, cancellationToken);
        filter(countQuery);
        using var connection = dbConnectionProvider.GetConnection();
        return await countQuery.SingleAsync<long>(connection);
    }

    private async Task ApplyTenantFilterAsync(ParameterizedQuery query, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        if (tenantAgnostic)
            return;

        var tenant = await tenantResolver.GetTenantAsync(cancellationToken);
        var tenantId = tenant?.Id;
        query.Is(nameof(Record.TenantId), (object?)tenantId ?? DBNull.Value);
    }

    private async Task SetTenantIdAsync(T record, CancellationToken cancellationToken)
    {
        if (record is not Record recordWithTenant)
            return;

        var tenant = await tenantResolver.GetTenantAsync(cancellationToken);
        var tenantId = tenant?.Id;
        recordWithTenant.TenantId = tenantId;
    }
}