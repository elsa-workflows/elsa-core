using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extension methods to perform bulk upsert operations for entities
/// in an Entity Framework Core context, supporting multiple database providers.
/// </summary>
public static class BulkUpsertExtensions
{
    internal const int DefaultBatchSize = 50;

    /// <summary>
    /// Performs a bulk upsert operation on a list of entities in the specified database context using a key selector.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <typeparam name="TEntity">The type of the entity being upserted.</typeparam>
    /// <param name="dbContext">The database context where the bulk upsert operation will be executed.</param>
    /// <param name="entities">The list of entities to be upserted.</param>
    /// <param name="keySelector">An expression used to determine the key for upsert operations.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        await BulkUpsertAsync(dbContext, entities, keySelector, DefaultBatchSize, cancellationToken);
    }

    /// <summary>
    /// Performs a bulk upsert operation on a list of entities in the specified database context using a key selector and optional batch size.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <typeparam name="TEntity">The type of the entity being upserted.</typeparam>
    /// <param name="dbContext">The database context where the bulk upsert operation will be executed.</param>
    /// <param name="entities">The list of entities to be upserted.</param>
    /// <param name="keySelector">An expression used to determine the key for upsert operations.</param>
    /// <param name="batchSize">The size of each batch for processing the upsert operation. Defaults to 50.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <exception cref="NotSupportedException">Thrown if the database provider for the context is not supported.</exception>
    public static async Task BulkUpsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        if (entities.Count == 0)
            return;

        // Identify the current provider (e.g., "Microsoft.EntityFrameworkCore.SqlServer")
        var providerName = dbContext.Database.ProviderName?.ToLowerInvariant() ?? string.Empty;

        if (providerName.Contains("mysql"))
        {
            await UpsertMySqlAsync(dbContext, entities, keySelector, cancellationToken);
            return;
        }

        // Determine the method for generating SQL based on the provider
        Func<DbContext, IList<TEntity>, Expression<Func<TEntity, string>>, (string, object[])> generateSql = providerName switch
        {
            var pn when pn.Contains("sqlserver") => GenerateSqlServerUpsert,
            var pn when pn.Contains("sqlite") => GenerateSqliteUpsert,
            var pn when pn.Contains("postgres") => GeneratePostgresUpsert,
            var pn when pn.Contains("oracle") => GenerateOracleUpsert,
            _ => throw new NotSupportedException($"Provider '{providerName}' is not supported.")
        };

        // Loop through batched entities
        foreach (var batch in entities.Chunk(batchSize))
        {
            // Generate SQL and parameters
            var (sql, parameters) = generateSql(dbContext, batch, keySelector);

            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }
    }

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
        var columnNames = props
            .Select(p => $"[{p.GetColumnName(storeObject)}]")
            .ToList();

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

                // If it's a shadow property, retrieve value via Entry(..).Property(..)
                var value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                var converter = property.GetTypeMapping().Converter;
                if (converter != null)
                    value = converter.ConvertToProvider(value)!;

                // Explicitly cast null values for varbinary columns
                if (property.GetColumnType().StartsWith("varbinary", StringComparison.OrdinalIgnoreCase) && value is null)
                    values.Add("CAST(NULL AS varbinary(max))"); // Explicitly cast null
                else
                    values.Add(paramName);
                
                parameters.Add(value!);
            }

            var line = $"({string.Join(", ", values)}){(i < entities.Count - 1 ? "," : string.Empty)}";
            mergeSql.AppendLine(line);
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
        var columnNames = props
            .Select(p => p.GetColumnName(storeObject)!)
            .ToList();

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
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                placeholders.Add(paramName);
                parameters.Add(value!);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1)
                sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine($"ON CONFLICT(\"{keyColumnName}\") DO UPDATE SET");

        var updateAssignments = columnNames
            .Where(c => c != keyColumnName)
            .Select(c => $"\"{c}\"=excluded.\"{c}\"");

        sb.AppendLine(string.Join(", ", updateAssignments) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

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
        var columnNames = props
            .Select(p => p.GetColumnName(storeObject)!)
            .ToList();

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
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                // Detect json/jsonb column types and cast the parameter so PostgreSQL accepts it.
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
            if (i < entities.Count - 1)
                sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine($"ON CONFLICT (\"{keyColumnName}\") DO UPDATE SET");

        var updateAssignments = columnNames
            .Where(c => c != keyColumnName)
            .Select(c => $"\"{c}\" = EXCLUDED.\"{c}\"");

        sb.AppendLine(string.Join(", ", updateAssignments) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

    private static async Task UpsertMySqlAsync<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var properties = entityType.GetProperties().ToList();
        var keyProperty = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyIndex = properties.IndexOf(keyProperty);

        static string Quote(string name) => $"`{name.Replace("`", "``")}`";
        var tableName = Quote(table.Name);
        var columns = properties.Select(property => Quote(property.GetColumnName(table)!)).ToArray();
        var placeholders = Enumerable.Range(0, properties.Count).Select(index => $"{{{index}}}").ToArray();
        var assignments = columns.Where((_, index) => index != keyIndex)
            .Select((column, index) => $"{column} = {placeholders[index >= keyIndex ? index + 1 : index]}");
        var lookup = $"SELECT 1 AS Value FROM {tableName} WHERE {columns[keyIndex]} = {{0}} FOR UPDATE";
        var insert = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", placeholders)})";
        var update = $"UPDATE {tableName} SET {string.Join(", ", assignments)} WHERE {columns[keyIndex]} = {placeholders[keyIndex]}";

        // MySQL's ON DUPLICATE KEY UPDATE can target a row reached through any unique index.
        // Lock and select by the requested key, then let ordinary writes reject other conflicts.
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var transaction = dbContext.Database.CurrentTransaction == null && System.Transactions.Transaction.Current == null
                ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
                : null;

            foreach (var entity in entities)
            {
                var parameters = properties.Select(property =>
                {
                    var value = property.IsShadowProperty()
                        ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                        : property.PropertyInfo?.GetValue(entity);
                    var converter = property.GetTypeMapping().Converter;
                    return (converter != null ? converter.ConvertToProvider(value) : value) ?? DBNull.Value;
                }).ToArray();

                var matches = await dbContext.Database.SqlQueryRaw<int>(lookup, parameters[keyIndex]).ToListAsync(cancellationToken);
                if (matches.Count > 1)
                    throw new InvalidOperationException("The MySQL upsert key matched multiple records.");

                if (matches.Count == 0)
                    await dbContext.Database.ExecuteSqlRawAsync(insert, parameters, cancellationToken);
                else if (properties.Count > 1)
                    await dbContext.Database.ExecuteSqlRawAsync(update, parameters, cancellationToken);
            }

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    internal static (string, object[]) GenerateOracleUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName()!;
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);
        var sqlGenerationHelper = dbContext.GetService<ISqlGenerationHelper>();
        var fullName = sqlGenerationHelper.DelimitIdentifier(tableName, schema);

        var props = entityType.GetProperties().ToList();

        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject)!;

        // Pre-build quoted column names once and reuse throughout all clauses.
        var quotedColumnNames = props
            .Select(p => sqlGenerationHelper.DelimitIdentifier(p.GetColumnName(storeObject)!))
            .ToList();
        var quotedKeyColumnName = sqlGenerationHelper.DelimitIdentifier(keyColumnName);

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

                // Aliases must be quoted so Oracle preserves their case, matching
                // the quoted references in ON, UPDATE SET, and INSERT/VALUES below.
                var quotedAlias = sqlGenerationHelper.DelimitIdentifier(property.GetColumnName(storeObject)!);

                // SELECT ... FROM DUAL has no target column to supply bind types.
                // Match text binds to the column's character set and type null scalars
                // explicitly before combining the source rows with UNION ALL.
                var columnType = property.GetColumnType() ?? string.Empty;
                // Null scalar values otherwise bind as strings, conflicting with non-null
                // numbers, timestamps or booleans in another UNION ALL row.
                var providerClrType = converter?.ProviderClrType ?? property.ClrType;
                var requiresCast = providerClrType == typeof(string) || (value is null && providerClrType.IsValueType);
                // Normalize LOB and JSON sources for every row, including nulls, so UNION ALL
                // sees consistent datatypes and character sets regardless of bind inference.
                var expr = columnType.ToUpperInvariant() switch
                {
                    "NCLOB" or "JSON" => $"TO_NCLOB({paramName})",
                    "CLOB" => $"TO_CLOB({paramName})",
                    "BLOB" => $"TO_BLOB({paramName})",
                    _ when requiresCast => $"CAST({paramName} AS {columnType})",
                    _ => paramName
                };

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
}
