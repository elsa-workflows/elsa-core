using Dapper;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using JetBrains.Annotations;

namespace Elsa.Dapper.Services;

/// <summary>
/// Provides a generic store using Dapper.
/// </summary>
[PublicAPI]
public class Store<T> where T : notnull
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Store{T}"/> class.
    /// </summary>
    public Store(IDbConnectionProvider dbConnectionProvider, string tableName, string primaryKey = "Id")
    {
        _dbConnectionProvider = dbConnectionProvider;
        TableName = tableName;
        PrimaryKey = primaryKey;
    }

    /// <summary>
    /// The name of the table.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// The name of the primary key column.
    /// </summary>
    public string PrimaryKey { get; }

    /// <summary>
    /// Finds a single record.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The record, if found.</returns>
    public async Task<T?> FindAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        var query = _dbConnectionProvider.CreateQuery().From(TableName);
        filter(query);
        using var connection = _dbConnectionProvider.GetConnection();
        return await query.FirstOrDefaultAsync<T>(connection);
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
        using var connection = _dbConnectionProvider.GetConnection();
        var query = _dbConnectionProvider.CreateQuery().From(TableName);
        filter(query);
        query = query.OrderBy(orderKey, orderDirection);
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
    public async Task<Page<T>> FindManyAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default) =>
        await FindManyAsync<T>(filter, pageArgs, orderKey, orderDirection, cancellationToken);

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
        return await FindManyAsync<T>(filter, pageArgs, orderFields, cancellationToken);
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
        return await FindManyAsync<TShape>(filter, pageArgs, new[] { new OrderField(orderKey, orderDirection) }, cancellationToken);
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
        using var connection = _dbConnectionProvider.GetConnection();

        var query = _dbConnectionProvider.CreateQuery().From(TableName);
        filter(query);
        query = query.OrderBy(orderFields.ToArray()).Page(pageArgs);

        var countQuery = _dbConnectionProvider.CreateQuery().Count(TableName);
        filter(countQuery);

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
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default) => await FindManyAsync<T>(filter, cancellationToken);

    /// <summary>
    /// Returns a set of records in the specified shape.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// /// <typeparam name="TShape">The shape type.</typeparam>
    /// <returns>A set of records.</returns>
    public async Task<IEnumerable<TShape>> FindManyAsync<TShape>(Action<ParameterizedQuery> filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = _dbConnectionProvider.CreateQuery().From(TableName);
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
    public async Task<IEnumerable<T>> FindManyAsync(Action<ParameterizedQuery> filter, string orderKey, OrderDirection orderDirection, CancellationToken cancellationToken = default) =>
        await FindManyAsync<T>(filter, orderKey, orderDirection, cancellationToken);

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
        using var connection = _dbConnectionProvider.GetConnection();
        var query = _dbConnectionProvider.CreateQuery().From(TableName);
        filter(query);
        query = query.OrderBy(orderKey, orderDirection);
        return await query.QueryAsync<TShape>(connection);
    }

    /// <summary>
    /// Saves the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="primaryKey">The primary key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(T record, string primaryKey = "Id", CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Upsert(TableName, primaryKey, record);
        await query.ExecuteAsync(connection);
    }

    /// <summary>
    /// Saves the specified records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <param name="primaryKey">The primary key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<T> records, string primaryKey = "Id", CancellationToken cancellationToken = default)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect);
        var currentIndex = 0;

        foreach (var record in records)
        {
            var index = currentIndex;
            query.Upsert(TableName, primaryKey, record, field => $"{field}_{index}");
            currentIndex++;
        }

        using var connection = _dbConnectionProvider.GetConnection();
        await query.ExecuteAsync(connection);
    }
    
    /// <summary>
    /// Adds the specified record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddAsync(T record, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Insert(TableName, record);
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
        var query = _dbConnectionProvider.CreateQuery().Delete(TableName);
        filter(query);
        using var connection = _dbConnectionProvider.GetConnection();
        return await query.ExecuteAsync(connection);
    }
    
    /// <summary>
    /// Deletes all records matching the specified query.
    /// </summary>
    /// <param name="filter">The conditions to apply to the query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="orderFields">The fields by which to order the results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    public async Task<long> DeleteAsync(Action<ParameterizedQuery> filter, PageArgs pageArgs, IEnumerable<OrderField> orderFields, CancellationToken cancellationToken = default)
    {
        var selectQuery =  _dbConnectionProvider.CreateQuery().From(TableName, "rowid");
        filter(selectQuery);
        selectQuery = selectQuery.OrderBy(orderFields.ToArray()).Page(pageArgs);
        
        var deleteQuery = _dbConnectionProvider.CreateQuery().Delete(TableName, selectQuery);
        using var connection = _dbConnectionProvider.GetConnection();
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
        var query = _dbConnectionProvider.CreateQuery().From(TableName, PrimaryKey);
        filter(query);
        using var connection = _dbConnectionProvider.GetConnection();
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
        var countQuery = _dbConnectionProvider.CreateQuery().Count(TableName);
        filter(countQuery);
        using var connection = _dbConnectionProvider.GetConnection();
        return await countQuery.SingleAsync<long>(connection);
    }
}