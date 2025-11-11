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
        await BulkUpsertAsync(dbContext, entities, keySelector, 50, cancellationToken);
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
        int batchSize = 50,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        if (entities.Count == 0)
            return;

        // Identify the current provider (e.g., "Microsoft.EntityFrameworkCore.SqlServer")
        var providerName = dbContext.Database.ProviderName?.ToLowerInvariant() ?? string.Empty;

        // Determine the method for generating SQL based on the provider
        Func<DbContext, IList<TEntity>, Expression<Func<TEntity, string>>, (string, object[])> generateSql = providerName switch
        {
            var pn when pn.Contains("sqlserver") => GenerateSqlServerUpsert,
            var pn when pn.Contains("sqlite") => GenerateSqliteUpsert,
            var pn when pn.Contains("postgres") => GeneratePostgresUpsert,
            var pn when pn.Contains("mysql") => GenerateMySqlUpsert,
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
                object? value = property.IsShadowProperty()
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

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                var converter = property.GetTypeMapping().Converter;
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                placeholders.Add(paramName);
                parameters.Add(value);
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

                object? value = property.IsShadowProperty()
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
                
                parameters.Add(value);
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
        var columnNames = props
            .Select(p => p.GetColumnName(storeObject)!)
            .ToList();

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

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                var converter = property.GetTypeMapping().Converter;
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                placeholders.Add(paramName);
                parameters.Add(value);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1)
                sb.Append(", ");
        }

        sb.AppendLine();
        sb.AppendLine("ON DUPLICATE KEY UPDATE");

        var updateAssignments = columnNames
            .Where(c => c != keyColumnName)
            .Select(c => $"`{c}` = VALUES(`{c}`)");

        sb.AppendLine(string.Join(", ", updateAssignments) + ";");

        return (sb.ToString(), parameters.ToArray());
    }

    private static (string, object[]) GenerateOracleUpsert<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName();
        var storeObject = StoreObjectIdentifier.Table(tableName!, schema);
        var fullName = !string.IsNullOrEmpty(schema) ? $"{schema}.{tableName}" : tableName;

        var props = entityType.GetProperties().ToList();

        var keyProp = entityType.FindProperty(keySelector.GetMemberAccess().Name)!;
        var keyColumnName = keyProp.GetColumnName(storeObject);

        var columnNames = props
            .Select(p => p.GetColumnName(storeObject)!)
            .ToList();

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

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                var converter = property.GetTypeMapping().Converter;
                if (converter != null)
                    value = converter.ConvertToProvider(value);

                parameters.Add(value);

                // Oracle aliases must match the column name
                var alias = property.GetColumnName(storeObject);
                lineParts.Add($"{paramName} AS {alias}");
            }

            // Comma if not last
            var suffix = (i < entities.Count - 1) ? " FROM DUAL UNION ALL SELECT" : " FROM DUAL";
            sb.AppendLine(string.Join(", ", lineParts) + suffix);
        }

        sb.AppendLine($") Source ON (Target.{keyColumnName} = Source.{keyColumnName})");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET");

        var updateSetClauses = columnNames
            .Where(c => c != keyColumnName)
            .Select(c => $"Target.{c} = Source.{c}");

        sb.AppendLine(string.Join(", ", updateSetClauses));
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"INSERT ({string.Join(", ", columnNames)})");
        sb.AppendLine($"VALUES ({string.Join(", ", columnNames.Select(c => $"Source.{c}"))});");

        return (sb.ToString(), parameters.ToArray());
    }
}