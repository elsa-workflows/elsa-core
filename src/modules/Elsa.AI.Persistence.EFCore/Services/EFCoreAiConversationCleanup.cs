using System.Runtime.CompilerServices;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

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
            var tableName = ResolveConversationTableName(dbContext);

            return await dbContext.Database.ExecuteSqlInterpolatedAsync(
                FormattableStringFactory.Create(
                    $$"""
                     DELETE FROM {{QuoteSqliteIdentifier(tableName)}}
                     WHERE "RetentionMode" = {0}
                       AND "RetentionExpiresAt" IS NOT NULL
                       AND "RetentionExpiresAt" <= {1}
                     """,
                    configuredRetentionMode,
                    now),
                cancellationToken);
        }

        return await dbContext.Conversations
            .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt != null && x.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string ResolveConversationTableName(AiDbContext dbContext)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(AiConversationRecord)) ?? throw new InvalidOperationException("AI conversation entity metadata was not found.");
        return entityType.GetTableName() ?? throw new InvalidOperationException("AI conversation table metadata was not found.");
    }

    private static string QuoteSqliteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
