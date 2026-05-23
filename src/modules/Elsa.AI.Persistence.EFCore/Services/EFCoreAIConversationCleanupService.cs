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
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to clean up expired AI conversations.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }
}
