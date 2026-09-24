using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Features;

public sealed class ConnectionLifecycleReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectionLifecycleReconciliationOptions> options,
    TimeProvider timeProvider,
    ILogger<ConnectionLifecycleReconciliationWorker> logger) : BackgroundService
{
    private const int MaximumTrackedScopes = 1024;
    private readonly Dictionary<(string TenantId, string EnvironmentId), ScopeCursorEntry> _cursors = new();
    private readonly LinkedList<(string TenantId, string EnvironmentId)> _scopeOrder = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var failed = false;
            if (options.Value.Enabled)
            {
                try
                {
                    failed = !await ProcessNextScopeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Exceptions can contain provider payloads. Keep logs free of exception text and candidate data.
                    logger.LogWarning("Credential lifecycle reconciliation cycle failed.");
                    failed = true;
                }
            }

            consecutiveFailures = failed ? Math.Min(consecutiveFailures + 1, 30) : 0;
            var delay = failed ? GetRetryDelay(consecutiveFailures) : options.Value.Interval;
            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<bool> ProcessNextScopeAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var scopeProvider = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleScopeProvider>();
        var target = await scopeProvider.GetNextScopeAsync(cancellationToken);
        if (target == null)
            return true;

        if (string.IsNullOrWhiteSpace(target.TenantId) || string.IsNullOrWhiteSpace(target.EnvironmentId))
        {
            logger.LogWarning("Credential lifecycle scope provider returned an invalid scope.");
            return false;
        }

        var scopeKey = (target.TenantId, target.EnvironmentId);
        var cursor = _cursors.TryGetValue(scopeKey, out var cached) ? cached.Cursor : null;
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        using var tenantContext = tenantAccessor.PushContext(new Tenant { Id = target.TenantId, Name = target.TenantId });
        var candidates = scope.ServiceProvider.GetRequiredService<IConnectionDueCandidateStore>();
        var page = await candidates.FindDueCandidatesAsync(
            target.TenantId, target.EnvironmentId, timeProvider.GetUtcNow(), options.Value.BatchSize, cursor, cancellationToken);

        RememberScope(scopeKey, page.NextCursor);
        if (page.Items.Count == 0)
            return true;

        var failures = 0;
        await Parallel.ForEachAsync(page.Items,
            new ParallelOptions { MaxDegreeOfParallelism = options.Value.MaxConcurrency, CancellationToken = cancellationToken },
            async (candidate, token) =>
            {
                if (candidate.TenantId != target.TenantId || candidate.EnvironmentId != target.EnvironmentId ||
                    string.IsNullOrWhiteSpace(candidate.ConnectionId) || candidate.DueAt > timeProvider.GetUtcNow())
                {
                    Interlocked.Increment(ref failures);
                    return;
                }

                try
                {
                    if (!await DispatchAsync(candidate, token))
                        Interlocked.Increment(ref failures);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Keep exceptions and candidate identifiers out of logs; provider errors may contain credential material.
                    Interlocked.Increment(ref failures);
                }
            });

        if (failures > 0)
        {
            logger.LogWarning("Credential lifecycle reconciliation page had {FailureCount} failed or out-of-scope candidates.", failures);
            return false;
        }

        return true;
    }

    private async Task<bool> DispatchAsync(ConnectionDueCandidate candidate, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        using var tenantContext = tenantAccessor.PushContext(new Tenant { Id = candidate.TenantId, Name = candidate.TenantId });
        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();

        switch (candidate.Kind)
        {
            case ConnectionDueCandidateKind.OAuthRefresh:
                return (await lifecycle.RefreshAsync(candidate.TenantId, candidate.EnvironmentId, candidate.ConnectionId, cancellationToken)).Succeeded;
            case ConnectionDueCandidateKind.ExpiredConnectionOperation:
            case ConnectionDueCandidateKind.RecoveryRequired:
                return (await lifecycle.ReconcileAsync(candidate.TenantId, candidate.EnvironmentId, candidate.ConnectionId, cancellationToken)).Succeeded;
            case ConnectionDueCandidateKind.GenerationCleanup:
                return (await lifecycle.CleanupGenerationAsync(candidate.TenantId, candidate.EnvironmentId,
                    candidate.ConnectionId, candidate.CandidateId, cancellationToken)).Succeeded;
            case ConnectionDueCandidateKind.Offboarding:
                return (await lifecycle.ReconcileOffboardingAsync(candidate.TenantId, candidate.EnvironmentId,
                    candidate.ConnectionId, cancellationToken)).Accepted;
            default:
                throw new InvalidOperationException("Unsupported credential lifecycle candidate kind.");
        }
    }

    private void RememberScope((string TenantId, string EnvironmentId) scopeKey, string? cursor)
    {
        if (_cursors.TryGetValue(scopeKey, out var existing))
        {
            _scopeOrder.Remove(existing.Node);
        }
        else
        {
            if (_cursors.Count >= MaximumTrackedScopes && _scopeOrder.First is { } oldest)
            {
                _scopeOrder.RemoveFirst();
                _cursors.Remove(oldest.Value);
            }
        }

        var node = _scopeOrder.AddLast(scopeKey);
        _cursors[scopeKey] = new ScopeCursorEntry(cursor, node);
    }

    private TimeSpan GetRetryDelay(int consecutiveFailures)
    {
        var configured = options.Value.RetryBackoff.TotalMilliseconds;
        var multiplier = Math.Pow(2, Math.Min(consecutiveFailures - 1, 20));
        return TimeSpan.FromMilliseconds(Math.Min(configured * multiplier, options.Value.MaxRetryBackoff.TotalMilliseconds));
    }

    private sealed record ScopeCursorEntry(string? Cursor, LinkedListNode<(string TenantId, string EnvironmentId)> Node);
}
