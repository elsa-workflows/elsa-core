using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Services;

public static class EFCoreAIConversationCleanup
{
    public static async ValueTask<int> DeleteExpiredAsync(AIDbContext dbContext, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var ephemeralRetentionMode = AIRetentionMode.Ephemeral.ToString();
        var configuredRetentionMode = AIRetentionMode.Configured.ToString();
        var completedStatus = AIConversationStatus.Completed.ToString();
        var failedStatus = AIConversationStatus.Failed.ToString();
        var expiredStatus = AIConversationStatus.Expired.ToString();

        var deletedEphemeral = await dbContext.Conversations
            .Where(x => x.RetentionMode == ephemeralRetentionMode && (x.Status == completedStatus || x.Status == failedStatus || x.Status == expiredStatus))
            .ExecuteDeleteAsync(cancellationToken);

        var deletedConfigured = await DeleteExpiredConfiguredAsync(dbContext, configuredRetentionMode, now, cancellationToken);

        return deletedEphemeral + deletedConfigured;
    }

    private static async ValueTask<int> DeleteExpiredConfiguredAsync(AIDbContext dbContext, string configuredRetentionMode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (IsSqliteProvider(dbContext.Database.ProviderName))
        {
            // EF Core SQLite cannot translate this DateTimeOffset predicate in ExecuteDeleteAsync for this model.
            var tableName = ResolveConversationTableName(dbContext);
            var sql = $@"DELETE FROM {QuoteSqliteIdentifier(tableName)}
WHERE ""RetentionMode"" = {{0}}
  AND ""RetentionExpiresAt"" IS NOT NULL
  AND ""RetentionExpiresAt"" <= {{1}}";
            return await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [configuredRetentionMode, now],
                cancellationToken);
        }

        return await dbContext.Conversations
            .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt != null && x.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string ResolveConversationTableName(AIDbContext dbContext)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(AIConversationRecord)) ?? throw new InvalidOperationException("AI conversation entity metadata was not found.");
        return entityType.GetTableName() ?? throw new InvalidOperationException("AI conversation table metadata was not found.");
    }

    private static bool IsSqliteProvider(string? providerName) =>
        providerName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

    private static string QuoteSqliteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
