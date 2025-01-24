using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Extensions;

public static class BulkInsertExtensions
{
    public static async Task BulkInsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
        where TEntity : class, new() =>
        await dbContext.BulkInsertAsync(entities, 50, cancellationToken);

    public static async Task BulkInsertAsync<TDbContext, TEntity>(
        this TDbContext dbContext,
        IList<TEntity> entities,
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
        Func<DbContext, IList<TEntity>, (string, object[])> generateSql = providerName switch
        {
            // var pn when pn.Contains("sqlserver") => GenerateSqlServerQuery,
            // var pn when pn.Contains("sqlite") => GenerateSqliteQuery,
            var pn when pn.Contains("postgres") => GeneratePostgresQuery,
            // var pn when pn.Contains("mysql") => GenerateMySqlQuery,
            // var pn when pn.Contains("oracle") => GenerateOracleQuery,
            _ => throw new NotSupportedException($"Provider '{providerName}' is not supported.")
        };

        // Loop through batched entities
        foreach (var batch in entities.Chunk(batchSize))
        {
            // Generate SQL and parameters
            var (sql, parameters) = generateSql(dbContext, batch);

            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }
    }

    private static (string, object[]) GenerateSqlServerQuery<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities,
        Expression<Func<TEntity, string>> keySelector)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = $"[{entityType.GetSchema()}].[{entityType.GetTableName()}]";
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        // Include shadow properties
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
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var values = new List<string>();

            for (var j = 0; j < props.Count; j++)
            {
                var property = props[j];
                var paramName = $"@p{i}_{j}";

                // If it's a shadow property, retrieve value via Entry(..).Property(..)
                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

                values.Add(paramName);
                parameters.Add(value);
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

    private static (string, object[]) GenerateSqliteQuery<TEntity>(
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

        sb.Append($"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var placeholders = new List<string>();

            for (var j = 0; j < props.Count; j++)
            {
                var property = props[j];
                var paramName = $"@p{i}_{j}";

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

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

    private static (string, object[]) GeneratePostgresQuery<TEntity>(
        DbContext dbContext,
        IList<TEntity> entities)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = entityType.GetTableName();
        var storeObject = StoreObjectIdentifier.Table(tableName!, entityType.GetSchema());

        var props = entityType.GetProperties().ToList();

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

                placeholders.Add(paramName);
                parameters.Add(value);
            }

            sb.Append($"({string.Join(", ", placeholders)})");
            if (i < entities.Count - 1)
                sb.Append(", ");
        }
        
        return (sb.ToString(), parameters.ToArray());
    }

    private static (string, object[]) GenerateMySqlQuery<TEntity>(
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

        sb.Append($"INSERT INTO `{tableName}` ({string.Join(", ", columnNames.Select(c => $"`{c}`"))}) VALUES ");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var placeholders = new List<string>();

            for (var j = 0; j < props.Count; j++)
            {
                var property = props[j];
                var paramName = $"@p{i}_{j}";

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

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

    private static (string, object[]) GenerateOracleQuery<TEntity>(
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

        sb.AppendLine($"MERGE INTO {fullName} Target");
        sb.AppendLine("USING (SELECT");

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var lineParts = new List<string>();

            for (var j = 0; j < props.Count; j++)
            {
                var property = props[j];
                var paramName = $":p{i}_{j}";

                object? value = property.IsShadowProperty()
                    ? dbContext.Entry(entity).Property(property.Name).CurrentValue
                    : property.PropertyInfo?.GetValue(entity);

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