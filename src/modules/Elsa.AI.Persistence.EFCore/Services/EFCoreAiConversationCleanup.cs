using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

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
        try
        {
            return await dbContext.Conversations
                .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt.HasValue && x.RetentionExpiresAt <= now)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase))
        {
            var sqlGenerationHelper = dbContext.GetService<ISqlGenerationHelper>();
            var entityType = dbContext.Model.FindEntityType(typeof(AiConversationRecord));
            var tableName = entityType?.GetTableName() ?? "AiConversations";
            var schema = entityType?.GetSchema();
            var table = schema == null
                ? sqlGenerationHelper.DelimitIdentifier(tableName)
                : sqlGenerationHelper.DelimitIdentifier(tableName, schema);
            var storeObject = StoreObjectIdentifier.Table(tableName, schema);
            var retentionModeColumnName = entityType?.FindProperty(nameof(AiConversationRecord.RetentionMode))?.GetColumnName(storeObject) ?? nameof(AiConversationRecord.RetentionMode);
            var retentionExpiresAtColumnName = entityType?.FindProperty(nameof(AiConversationRecord.RetentionExpiresAt))?.GetColumnName(storeObject) ?? nameof(AiConversationRecord.RetentionExpiresAt);
            var retentionMode = sqlGenerationHelper.DelimitIdentifier(retentionModeColumnName);
            var retentionExpiresAt = sqlGenerationHelper.DelimitIdentifier(retentionExpiresAtColumnName);
            var sql = $"DELETE FROM {table} WHERE {retentionMode} = {{0}} AND {retentionExpiresAt} IS NOT NULL AND {retentionExpiresAt} <= {{1}}";
            var parameters = new object[] { configuredRetentionMode, now };

            return await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }
    }
}
