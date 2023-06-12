using Elsa.Common.Entities;

namespace Elsa.Dapper.Contracts;

/// <summary>
/// Represents a SQL dialect.
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// Returns a SELECT FROM query.
    /// </summary>
    /// <param name="table">The table to query.</param>
    string From(string table);
    
    /// <summary>
    /// Returns a SELECT FROM query.
    /// </summary>
    /// <param name="table">The table to query.</param>
    /// <param name="fields">The fields to query.</param>
    string From(string table, params string[] fields);
    
    /// <summary>
    /// Returns a DELETE FROM query.
    /// </summary>
    /// <param name="table">The table to query.</param>
    string Delete(string table);
    
    /// <summary>
    /// Returns a SELECT count(*) FROM query.
    /// </summary>
    /// <param name="table">The table to query.</param>
    string Count(string table);
    
    /// <summary>
    /// Returns an AND clause.
    /// </summary>
    /// <param name="field">The field to query.</param>
    string And(string field);
    
    /// <summary>
    /// Returns an AND field IN () clause.
    /// </summary>
    /// <param name="field">The field to query.</param>
    /// <param name="fieldParamNames">The parameter names to query.</param>
    string And(string field, string[] fieldParamNames);
    
    /// <summary>
    /// Returns an ORDER BY clause.
    /// </summary>
    /// <param name="field">The field to order by.</param>
    /// <param name="direction">The direction to order by.</param>
    string OrderBy(string field, OrderDirection direction);
    
    /// <summary>
    /// Returns an OFFSET clause.
    /// </summary>
    /// <param name="count">The number of records to skip.</param>
    string Skip(int count);
    
    /// <summary>
    /// Returns a LIMIT clause.
    /// </summary>
    /// <param name="count">The number of records to take.</param>
    string Take(int count);

    /// <summary>
    /// Builds an INSERT query.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="fields">The fields.</param>
    /// <returns>The query.</returns>
    string Insert(string table, string[] fields);
    
    /// <summary>
    /// Builds an UPSERT query.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="primaryKeyField">The primary key field.</param>
    /// <param name="fields">The fields.</param>
    /// <returns>The query.</returns>
    string Upsert(string table, string primaryKeyField, string[] fields);
}