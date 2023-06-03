using System.Text;
using Dapper;
using Elsa.Common.Models;

namespace Elsa.Dapper.Extensions;

/// <summary>
/// Provides extension methods for <see cref="StringBuilder"/> to build SQL queries.
/// </summary>
public static class StringBuilderSqlExtensions
{
    /// <summary>
    /// Begins a SELECT FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <returns>The query.</returns>
    public static StringBuilder From(this StringBuilder query, string table)
    {
        query.AppendLine($"SELECT * FROM {table} WHERE 1=1");
        return query;
    }
    
    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="value">The value.</param>
    /// <returns>The query.</returns>
    public static StringBuilder And(this StringBuilder query, string field, DynamicParameters parameters, object? value)
    {
        if (value == null) return query;
        query.AppendLine($"AND {field} = @{field}");
        parameters.Add($"@{field}", value);
        
        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="values">The values.</param>
    /// <returns>The query.</returns>
    public static StringBuilder And(this StringBuilder query, string field, DynamicParameters parameters, object[]? values)
    {
        if (values == null || !values.Any()) return query;
        
        var fieldParamNames = values
            .Select((_, index) => $"@{field}{index}")
            .ToArray();

        query.AppendLine($"AND {field} IN ({string.Join(", ", fieldParamNames)})");

        for (var i = 0; i < fieldParamNames.Length; i++)
            parameters.Add(fieldParamNames[i], values.ElementAt(i));
        
        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <returns>The query.</returns>
    public static StringBuilder And(this StringBuilder query, DynamicParameters parameters, VersionOptions? versionOptions)
    {
        if(versionOptions == null) return query; 

        var options = versionOptions.Value;
        if (options.IsDraft) query.Append(" AND IsPublished = 0");
        if (options.IsLatest) query.Append(" AND IsLatest = 1");
        if (options.IsPublished) query.Append(" AND IsPublished = 1");
        if (options.IsLatestOrPublished) query.Append(" AND (IsLatest = 1 OR IsPublished = 1)");
        if (options.IsLatestAndPublished) query.Append(" AND IsLatest = 1 AND IsPublished = 1");
        if (options.Version > 0)
        {
            query.AppendLine("AND Version = @Version");
            parameters.Add("@Version", options.Version);
        }
        
        return query;
    }
}