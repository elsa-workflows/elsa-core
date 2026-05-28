using System.Data.Common;
using Elsa.AI.Persistence.EFCore.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.AI.Persistence.EFCore.Services;

public class EFCoreAIConversationCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<EFCoreAIConversationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AIDbContext>();
                await EFCoreAIConversationCleanup.DeleteExpiredAsync(dbContext, DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (InvalidOperationException e)
            {
                LogCleanupFailure(e);
            }
            catch (DbUpdateException e)
            {
                LogCleanupFailure(e);
            }
            catch (DbException e)
            {
                LogCleanupFailure(e);
            }
            catch (Exception e) when (ExceptionFilters.IsNonFatal(e))
            {
                LogCleanupFailure(e);
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private void LogCleanupFailure(Exception e)
    {
        logger.LogWarning(e, "Failed to clean up expired AI conversations.");
    }
}
