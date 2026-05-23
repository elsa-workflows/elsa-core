using Elsa.AI.Abstractions.Models;
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
        try
        {
            return await dbContext.Conversations
                .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt.HasValue && x.RetentionExpiresAt <= now)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase))
        {
            var expiredConfigured = await dbContext.Conversations
                .Where(x => x.RetentionMode == configuredRetentionMode && x.RetentionExpiresAt.HasValue)
                .ToListAsync(cancellationToken);
            expiredConfigured = expiredConfigured
                .Where(x => x.RetentionExpiresAt <= now)
                .ToList();
            dbContext.Conversations.RemoveRange(expiredConfigured);
            return expiredConfigured.Count == 0 ? 0 : await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
