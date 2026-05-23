using System.Runtime.CompilerServices;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.AI.Persistence.EFCore.Services;

public static class EFCoreAiConversationCleanup
{
    public static async ValueTask<int> DeleteExpiredAsync(AiDbContext dbContext, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var ephemeralRetentionMode = AiRetentionMode.Ephemeral.ToString();
        var configuredRetentionMode = AiRetentionMode.Configured.ToString();
        var completedStatus = AiConversationStatus.Completed.ToString();
        var failedStatus = AiConversationStatus.Failed.ToString();

        var deletedEphemeral = await dbContext.Conversations
            .Where(x => x.RetentionMode == ephemeralRetentionMode && (x.Status == completedStatus || x.Status == failedStatus))
            .ExecuteDeleteAsync(cancellationToken);

        var deletedConfigured = await DeleteExpiredConfiguredAsync(dbContext, configuredRetentionMode, now, cancellationToken);

        return deletedEphemeral + deletedConfigured;
    }

    private static async ValueTask<int> DeleteExpiredConfiguredAsync(AiDbContext dbContext, string configuredRetentionMode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            var table = ResolveConversationTable(dbContext);
            var retentionModeColumn = ResolveConversationColumn(table.EntityType, table.StoreObject, nameof(AiConversationRecord.RetentionMode));
            var retentionExpiresAtColumn = ResolveConversationColumn(table.EntityType, table.StoreObject, nameof(AiConversationRecord.RetentionExpiresAt));

            return await dbContext.Database.ExecuteSqlInterpolatedAsync(
                FormattableStringFactory.Create(
                    $$"""
                     DELETE FROM {{QuoteSqliteIdentifier(table.Name)}}
                     WHERE {{retentionModeColumn}} = {0}
                       AND {{retentionExpiresAtColumn}} IS NOT NULL
                       AND {{retentionExpiresAtColumn}} <= {1}
                     """,
                    configuredRetentionMode,
                    now),
                cancellationToken);
        }

        return await dbContext.Conversations
            .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt != null && x.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static (IEntityType EntityType, StoreObjectIdentifier StoreObject, string Name) ResolveConversationTable(AiDbContext dbContext)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(AiConversationRecord)) ?? throw new InvalidOperationException("AI conversation entity metadata was not found.");
        var tableName = entityType.GetTableName() ?? throw new InvalidOperationException("AI conversation table metadata was not found.");
        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        return (entityType, storeObject, tableName);
    }

    private static string ResolveConversationColumn(IEntityType entityType, StoreObjectIdentifier storeObject, string propertyName)
    {
        var property = entityType.FindProperty(propertyName) ?? throw new InvalidOperationException($"AI conversation property metadata was not found for {propertyName}.");
        var columnName = property.GetColumnName(storeObject) ?? throw new InvalidOperationException($"AI conversation column metadata was not found for {propertyName}.");
        return QuoteSqliteIdentifier(columnName);
    }

    private static string QuoteSqliteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
