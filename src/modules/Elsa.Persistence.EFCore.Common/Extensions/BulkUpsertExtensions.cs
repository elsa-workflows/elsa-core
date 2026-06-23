using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extension methods to perform bulk upsert operations for entities
/// in an Entity Framework Core context, supporting multiple database providers.
/// </summary>
public static class BulkUpsertExtensions
{
    /// <summary>
    /// Performs a bulk upsert operation on a list of entities in the specified database context using a key selector.
    /// </summary>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        await BulkUpsertAsync(dbContext, entities, keySelector, 50, cancellationToken);
    }

    /// <summary>
    /// Performs a bulk upsert operation on a list of entities in the specified database context using a key selector and optional batch size.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the database provider for the context is not supported.</exception>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        int batchSize = 50,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        if (entities.Count == 0)
            return;

        var providerName = dbContext.Database.ProviderName?.ToLowerInvariant() ?? string.Empty;

        Func<DbContext, IList<TEntity>, Expression<Func<TEntity, string>>, (string, object[])> generateSql = providerName switch
        {
            var pn when pn.Contains("sqlserver") => GenerateSqlServerUpsert,
            var pn when pn.Contains("sqlite")    => GenerateSqliteUpsert,
            var pn when pn.Contains("postgres")  => GeneratePostgresUpsert,
            var pn when pn.Contains("mysql")     => GenerateMySqlUpsert,
            var pn when pn.Contains("oracle")    => GenerateOracleUpsert,
            _ => throw new NotSupportedException($"Provider '{providerName}' is not supported.")
        };

        foreach (var batch in entities.Chunk(batchSize))
        {
            var (sql, parameters) = generateSql(dbContext, batch, keySelector);
            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }
    }

    // -------------------------------------------------------------------------
    // SQL Server
    // -------------------------------------------------------------------------

    private static (string, object[]) GenerateSqlServerUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = $"[{entityType.GetSchema()}].[{entityType.GetTableName()}]";
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var props = entityType.GetProperties().ToList();
        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = $"[{keyProp.GetColumnName(storeObject)}]";
        var columnNames = props.Select(p => $"[{p.GetColumnName(storeObject)}]").ToList();

        var mergeSql = new StringBuilder();
        mergeSql.AppendLine($"MERGE {tableName} AS Target");
        mergeSql.AppendLine("USING (VALUES");

        var parameters = new List<object>();
        var parameterCount = 0;

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var values = new List<string>();

            foreach (var property in props)
            {
                var paramName = $"{{{parameterCount++}}}";
                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);
                var converter = property.GetTypeMapping().Converter;
                if (converter != null) value = converter.ConvertToProvider(value)!;

                if (property.GetColumnType().StartsWith("varbinary", StringComparison.OrdinalIgnoreCase) && value is null)
                    values.Add("CAST(NULL AS varbinary(max))");
                else
                    values.Add(paramName);

                parameters.Add(value!);
            }

            mergeSql.AppendLine($"({string.Join(", ", values)}){(i < entities.Count - 1 ? "," : string.Empty)}");
        }

        mergeSql.AppendLine($") AS Source ({string.Join(", ", columnNames)})");
        mergeSql.AppendLine($"ON Target.{keyColumnName} = Source.{keyColumnName}");
        mergeSql.AppendLine("WHEN MATCHED THEN");
        mergeSql.AppendLine($"    UPDATE SET {string.Join(", ", columnNames.Where(c => c != keyColumnName).Select(c => $"Target.{c} = Source.{c}"))}");
        mergeSql.AppendLine("WHEN NOT MATCHED THEN");
        mergeSql.AppendLine($"    INSERT ({string.Join(", ", columnNames)})");
        mergeSql.AppendLine($"    VALUES ({string.Join(", ", columnNames.Select(c => $"Source.{c}"))});");

        return (mergeSql.ToString(), parameters.ToArray());
    }

    // -------------------------------------------------------------------------
    // SQLite
    // -------------------------------------------------------------------------

    private static (string, object[]) GenerateSqliteUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = entityType.GetTableName();
        var storeObject = StoreObjectIdentifier.Table(tableName!, entityType.GetSchema());
        var props = entityType.GetProperties().ToList();
        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject);
        var columnNames = props.Select(p => p.GetColumnName(storeObject)!).ToList();

        var sb = new StringBuilder();
        var parameters = new List<object>();
        var parameterCount = 0;

        sb.Append($"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var placeholders = new List<string>();

            foreach (var property in props)
            {
                var paramName = $"{{{parameterCount++}}}";
                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);
                var converter = property.GetTypeMapping().Converter;
                if (converter != null) value = converter.ConvertToProvider(value);
                placeholders.Add(paramName);
                parameters.Add(value!);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1) sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine($"ON CONFLICT(\"{keyColumnName}\") DO UPDATE SET");
        sb.AppendLine(string.Join(", ", columnNames.Where(c => c != keyColumnName).Select(c => $"\"{c}\"=excluded.\"{c}\"")) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

    // -------------------------------------------------------------------------
    // PostgreSQL
    // -------------------------------------------------------------------------

    private static (string, object[]) GeneratePostgresUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = entityType.GetTableName();
        var storeObject = StoreObjectIdentifier.Table(tableName!, entityType.GetSchema());
        var props = entityType.GetProperties().ToList();
        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject);
        var columnNames = props.Select(p => p.GetColumnName(storeObject)!).ToList();

        var sb = new StringBuilder();
        var parameters = new List<object>();
        var parameterCount = 0;

        sb.Append($"INSERT INTO \"{storeObject.Schema}\".\"{storeObject.Name}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var placeholders = new List<string>();

            foreach (var property in props)
            {
                var paramName = $"{{{parameterCount++}}}";
                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);
                var converter = property.GetTypeMapping().Converter;
                if (converter != null) value = converter.ConvertToProvider(value);

                var columnType = property.GetColumnType();
                if (columnType.StartsWith("jsonb", StringComparison.OrdinalIgnoreCase))
                    placeholders.Add($"CAST({paramName} AS jsonb)");
                else if (columnType.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    placeholders.Add($"CAST({paramName} AS json)");
                else
                    placeholders.Add(paramName);

                parameters.Add(value!);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1) sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine($"ON CONFLICT (\"{keyColumnName}\") DO UPDATE SET");
        sb.AppendLine(string.Join(", ", columnNames.Where(c => c != keyColumnName).Select(c => $"\"{c}\" = EXCLUDED.\"{c}\"")) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

    // -------------------------------------------------------------------------
    // MySQL
    // -------------------------------------------------------------------------

    private static (string, object[]) GenerateMySqlUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = entityType.GetTableName();
        var storeObject = StoreObjectIdentifier.Table(tableName!, entityType.GetSchema());
        var props = entityType.GetProperties().ToList();
        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject);
        var columnNames = props.Select(p => p.GetColumnName(storeObject)!).ToList();

        var sb = new StringBuilder();
        var parameters = new List<object>();
        var parameterCount = 0;

        sb.Append($"INSERT INTO `{tableName}` ({string.Join(", ", columnNames.Select(c => $"`{c}`"))}) VALUES ");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var placeholders = new List<string>();

            foreach (var property in props)
            {
                var paramName = $"{{{parameterCount++}}}";
                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);
                var converter = property.GetTypeMapping().Converter;
                if (converter != null) value = converter.ConvertToProvider(value);
                placeholders.Add(paramName);
                parameters.Add(value!);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1) sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine("ON DUPLICATE KEY UPDATE");
        sb.AppendLine(string.Join(", ", columnNames.Where(c => c != keyColumnName).Select(c => $"`{c}` = VALUES(`{c}`)")) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

    // -------------------------------------------------------------------------
    // Oracle
    // -------------------------------------------------------------------------

    private static (string, object[]) GenerateOracleUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName()!;
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);

        // Both schema and table must be quoted so Oracle treats them as
        // case-sensitive identifiers, matching what EF Core migrations create.
        var fullName = !string.IsNullOrEmpty(schema)
            ? $"\"{schema}\".\"{tableName}\""
            : $"\"{tableName}\"";

        var props = entityType.GetProperties().ToList();
        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject)!;

        // Pre-build quoted column name list once; reuse throughout.
        var quotedColumnNames = props
            .Select(p => $"\"{p.GetColumnName(storeObject)}\"")
            .ToList();
        var quotedKeyColumnName = $"\"{keyColumnName}\"";

        var sb = new StringBuilder();
        var parameters = new List<object>();
        var parameterCount = 0;

        sb.AppendLine($"MERGE INTO {fullName} Target");
        sb.AppendLine("USING (SELECT");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var lineParts = new List<string>();

            foreach (var property in props)
            {
                var paramName = $"{{{parameterCount++}}}";

                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                var converter = property.GetTypeMapping().Converter;
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                parameters.Add(value!);

                // Alias must be quoted so Oracle preserves case, matching the
                // quoted references in the WHEN MATCHED / WHEN NOT MATCHED clauses.
                var quotedAlias = $"\"{property.GetColumnName(storeObject)}\"";

                // Oracle cannot infer the bind parameter type from a bare SELECT …
                // FROM DUAL — there is no target column to derive it from. For
                // NVARCHAR2 columns this causes ODP.NET to default to VARCHAR2,
                // which leads to datatype mismatch errors in the MERGE. An explicit
                // CAST restores the correct type. The length is read from the EF
                // column type string (e.g. "NVARCHAR2(500)") so it matches the
                // actual column definition rather than an arbitrary hardcoded value.
                string expr;
                var columnType = property.GetColumnType() ?? string.Empty;
                if (columnType.StartsWith("NVARCHAR2", StringComparison.OrdinalIgnoreCase))
                {
                    var length = ParseNVarchar2Length(columnType);
                    expr = $"CAST({paramName} AS NVARCHAR2({length}))";
                }
                else
                {
                    expr = paramName;
                }

                lineParts.Add($"{expr} AS {quotedAlias}");
            }

            var suffix = i < entities.Count - 1 ? " FROM DUAL UNION ALL SELECT" : " FROM DUAL";
            sb.AppendLine(string.Join(", ", lineParts) + suffix);
        }

        sb.AppendLine($") Source ON (Target.{quotedKeyColumnName} = Source.{quotedKeyColumnName})");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET");
        sb.AppendLine(string.Join(", ", quotedColumnNames
            .Where(c => c != quotedKeyColumnName)
            .Select(c => $"Target.{c} = Source.{c}")));
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"INSERT ({string.Join(", ", quotedColumnNames)})");
        sb.AppendLine($"VALUES ({string.Join(", ", quotedColumnNames.Select(c => $"Source.{c}"))});");

        return (sb.ToString(), parameters.ToArray());
    }

    /// <summary>
    /// Extracts the maximum length from an Oracle NVARCHAR2 column type string.
    /// For example, "NVARCHAR2(500)" returns 500.
    /// Falls back to 2000 (Oracle's maximum for NVARCHAR2) if the string is malformed.
    /// </summary>
    private static int ParseNVarchar2Length(string columnType)
    {
        var open = columnType.IndexOf('(');
        var close = columnType.IndexOf(')');
        if (open >= 0 && close > open &&
            int.TryParse(columnType.AsSpan(open + 1, close - open - 1), out var length))
            return length;

        return 2000;
    }
}
