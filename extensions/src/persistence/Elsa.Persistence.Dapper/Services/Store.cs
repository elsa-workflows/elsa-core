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
    /// Returns the records of one page without counting the total.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderKey">The order key selector.</param>
    /// <param name="orderDirection">The order direction.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The records of the page.</returns>
    public async Task<IEnumerable<T>> FindPageItemsAsync(Action<ParameterizedQuery>? filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var query = dbConnectionProvider.CreateQuery().From(TableName);
        ApplyTenantFilter(query, tenantAgnostic);
        filter?.Invoke(query);
        query = query.OrderBy(new OrderField(orderKey, orderDirection)).Page(pageArgs);

        using var connection = dbConnectionProvider.GetConnection();
        return await query.QueryAsync<T>(connection);
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
        {
            return;
        }

        if (affected < 0 && await ConfirmNegativeCountWriteAsync(record, connection))
        {
            return;
        }

        await ResolveZeroRowWriteAsync(record, connection, ExecuteUpsertAsync);
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
        {
            return;
        }

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
        // PostgreSQL, SQL Server, and Microsoft.Data.Sqlite all sum affected rows across
        // the statements of one command, so a full batch reports `recordCount`. SQL Server
        // with NOCOUNT ON reports -1, which is indeterminate: confirm each row is ours.
        if (affected == recordsList.Count)
        {
            return;
        }

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
        var affected = await ExecuteTenantScopedUpdateAsync(record, connection);
        if (affected > 0)
        {
            return;
        }

        await ResolveZeroRowUpdateAsync(record, connection);
    }
    
    public async Task UpdateAsync(T record, Expression<Func<T, object>>[] props, CancellationToken cancellationToken = default)
    {
        using var connection = dbConnectionProvider.GetConnection();
        SetTenantId(record);
        var fields = props.Select(x => x.GetPropertyName()).ToArray();
        var affected = await ExecuteTenantScopedUpdateAsync(record, connection, fields);
        if (affected > 0)
        {
            return;
        }

        await ResolveZeroRowUpdateAsync(record, connection);
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
        ApplyWriteTenantScope(query);
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

        ApplyWriteTenantScope(query);

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

        ApplyWriteTenantScope(selectQuery);
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
        return await CountAsync(filter, false, cancellationToken);
    }

    /// <summary>
    /// Returns the number of records matching the specified query.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="tenantAgnostic">Whether to ignore the tenant filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records matching the specified query.</returns>
    public async Task<long> CountAsync(Action<ParameterizedQuery> filter, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var countQuery = dbConnectionProvider.CreateQuery().Count(TableName);
        ApplyTenantFilter(countQuery, tenantAgnostic);
        filter(countQuery);
        using var connection = dbConnectionProvider.GetConnection();
        return await countQuery.SingleAsync<long>(connection);
    }

    /// <summary>
    /// Returns the distinct tenant ids stored in the table, across all tenants.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The distinct raw tenant id values. May contain <c>null</c> and empty strings.</returns>
    public async Task<IReadOnlyCollection<string?>> ListTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        var dialect = dbConnectionProvider.Dialect;
        var sql = $"select distinct {dialect.QuoteIdentifier(nameof(Record.TenantId))} from {dialect.QuoteIdentifier(TableName)}";
        using var connection = dbConnectionProvider.GetConnection();
        return (await connection.QueryAsync<string?>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    private void ApplyTenantFilter(ParameterizedQuery query, bool tenantAgnostic = false)
    {
        if (tenantAgnostic)
        {
            return;
        }

        // Mirror EF SetTenantIdFilter: ambient match OR '*' OR (NULL when ambient is default).
        ApplyTenantVisibility(query);
    }

    internal void ApplyTenantVisibility(ParameterizedQuery query)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        var ident = query.Dialect.QuoteIdentifier(nameof(Record.TenantId));

        if (string.IsNullOrEmpty(tenantId))
        {
            query.Sql.AppendLine($"and ({ident} is null or {ident} = '' or {ident} = @AgnosticTenantId)");
            query.Parameters.Add("@AgnosticTenantId", Tenant.AgnosticTenantId);
            return;
        }

        query.Sql.AppendLine($"and ({ident} = @AmbientTenantId or {ident} = @AgnosticTenantId)");
        query.Parameters.Add("@AmbientTenantId", tenantId);
        query.Parameters.Add("@AgnosticTenantId", Tenant.AgnosticTenantId);
    }

    /// <summary>
    /// Writes and deletes match the ambient tenant only. Shared <c>*</c> rows stay visible on
    /// reads but cannot be taken over, deleted, or filter-updated by a concrete tenant.
    /// </summary>
    private void ApplyWriteTenantScope(ParameterizedQuery query)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        if (string.IsNullOrEmpty(tenantId))
        {
            query.IsNullOrEmpty(nameof(Record.TenantId));
            return;
        }

        query.Is(nameof(Record.TenantId), tenantId);
    }

    private void ApplyOwnedTenantScope(ParameterizedQuery query, T record)
    {
        if (record is not Record owned)
        {
            return;
        }

        var ident = query.Dialect.QuoteIdentifier(nameof(Record.TenantId));
        query.Sql.AppendLine($"and COALESCE({ident}, '') = COALESCE(@OwnedTenantId, '')");
        query.Parameters.Add("@OwnedTenantId", owned.TenantId ?? string.Empty);
    }

    private void SetTenantId(T record)
    {
        if (record is not Record recordWithTenant)
        {
            return;
        }

        // Keep an explicit '*'. Stamp only when TenantId is null. Keep an explicit concrete
        // id only when it equals the ambient tenant or there is no ambient tenant. Do not
        // trust any other explicit concrete id.
        var ambient = tenantAccessor.Tenant;
        var ambientId = ambient?.Id;
        var explicitId = recordWithTenant.TenantId;

        if (explicitId == Tenant.AgnosticTenantId)
        {
            return;
        }

        if (string.IsNullOrEmpty(explicitId))
        {
            recordWithTenant.TenantId = ambientId;
            return;
        }

        if (ambient == null || SameTenant(explicitId, ambientId))
        {
            return;
        }

        throw new InvalidOperationException($"The record tenant '{explicitId}' does not match the current tenant '{ambientId}'.");
    }

    // Cross-tenant Id rule:
    // - Existing '*': a concrete tenant cannot take it over. Incoming '*' updates the shared
    //   row in place. Incoming ambient/concrete throws and leaves TenantId as '*'.
    // - Existing concrete tenant A, incoming other tenant B: throw.
    // - Same tenant, treating NULL and '' as the default tenant: retry the tenant-scoped upsert.
    // - Missing row after a 0-row upsert: retry once, then throw.
    // - Update of a missing row is a no-op (heartbeats). Updates never retry.
    // - A negative affected count (SQL Server NOCOUNT ON) is indeterminate: look the row up
    //   and throw if it belongs to another tenant.
    private async Task ResolveZeroRowWriteAsync(T record, IDbConnection connection, Func<T, IDbConnection, Task<int>> write)
    {
        var key = GetPrimaryKeyValue(record);
        var existing = await LookupByKeyAsync(key, connection);
        OnAfterExistingRowLookup(record, existing);

        if (existing is Record owned && !CanWriteExistingRow(owned, record))
        {
            throw CrossTenantConflict(key);
        }

        var retried = await write(record, connection);
        if (retried > 0)
        {
            return;
        }

        if (retried < 0 && await ConfirmNegativeCountWriteAsync(record, connection))
        {
            return;
        }

        throw new InvalidOperationException($"Failed to save a record with ID '{key}'.");
    }

    private async Task ResolveZeroRowUpdateAsync(T record, IDbConnection connection)
    {
        var key = GetPrimaryKeyValue(record);
        var existing = await LookupByKeyAsync(key, connection);
        OnAfterExistingRowLookup(record, existing);

        if (existing is Record owned && !CanWriteExistingRow(owned, record))
        {
            throw CrossTenantConflict(key);
        }
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
                {
                    existingByKey[existingKey] = existing;
                }
            }
        }

        foreach (var record in records)
        {
            var key = GetPrimaryKeyValue(record);
            if (key != null && existingByKey.TryGetValue(key, out var existing) && existing is Record owned && !CanWriteExistingRow(owned, record))
            {
                throw CrossTenantConflict(key);
            }

            var retried = await ExecuteUpsertAsync(record, connection);
            if (retried > 0)
            {
                continue;
            }

            if (retried < 0 && await ConfirmNegativeCountWriteAsync(record, connection))
            {
                continue;
            }

            throw new InvalidOperationException($"Failed to save a record with ID '{key}'.");
        }
    }

    private async Task<bool> ConfirmNegativeCountWriteAsync(T record, IDbConnection connection)
    {
        var existing = await LookupByKeyAsync(GetPrimaryKeyValue(record), connection);
        if (existing is Record owned && !CanWriteExistingRow(owned, record))
        {
            throw CrossTenantConflict(GetPrimaryKeyValue(record));
        }

        return existing != null;
    }

    private static InvalidOperationException CrossTenantConflict(object? key) =>
        new($"A record with ID '{key}' already exists in another tenant.");

    private static bool CanWriteExistingRow(Record owned, T record)
    {
        var incomingTenantId = record is Record incoming ? incoming.TenantId : null;

        if (owned.TenantId == Tenant.AgnosticTenantId)
        {
            // Shared rows stay shared. A concrete tenant cannot restamp '*' to itself.
            return incomingTenantId == Tenant.AgnosticTenantId;
        }

        return SameTenant(owned.TenantId, incomingTenantId);
    }

    private async Task<int> ExecuteUpsertAsync(T record, IDbConnection connection)
    {
        if (ConsumeForcedAffectedRows() is { } forced)
        {
            return forced;
        }

        var query = new ParameterizedQuery(dbConnectionProvider.Dialect).Upsert(TableName, PrimaryKey, record);
        return await query.ExecuteAsync(connection);
    }

    private async Task<int> ExecuteTenantScopedUpdateAsync(T record, IDbConnection connection, string[]? fields = null)
    {
        if (ConsumeForcedAffectedRows() is { } forced)
        {
            return forced;
        }

        fields ??= record.GetType().GetProperties()
            .Where(x => x.CanRead && x.Name != PrimaryKey)
            .Select(x => x.Name)
            .ToArray();

        var key = GetPrimaryKeyValue(record);
        if (key == null)
        {
            throw new InvalidOperationException($"Cannot update a record without a {PrimaryKey}.");
        }

        var query = new ParameterizedQuery(dbConnectionProvider.Dialect)
            .Update(TableName, record, fields)
            .Is(PrimaryKey, key);
        ApplyOwnedTenantScope(query, record);
        return await query.ExecuteAsync(connection);
    }

    private async Task<T?> LookupByKeyAsync(object? key, IDbConnection connection)
    {
        if (key == null)
        {
            return default;
        }

        var lookup = new ParameterizedQuery(dbConnectionProvider.Dialect).From(TableName);
        lookup.Is(PrimaryKey, key);
        return await lookup.FirstOrDefaultAsync<T>(connection);
    }

    private object? GetPrimaryKeyValue(T record) =>
        record.GetType().GetProperty(PrimaryKey)?.GetValue(record);

    private static bool SameTenant(string? left, string? right) =>
        string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);

    internal virtual int? ConsumeForcedAffectedRows() => null;

    internal virtual void OnAfterExistingRowLookup(T record, T? existing)
    {
    }
}