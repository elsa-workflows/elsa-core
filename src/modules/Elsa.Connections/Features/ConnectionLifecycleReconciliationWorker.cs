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
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.Enabled)
            {
                try
                {
                    await ProcessNextScopeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Exceptions can contain provider payloads. Keep logs free of exception text and candidate data.
                    logger.LogWarning("Credential lifecycle reconciliation cycle failed.");
                }
            }

            try
            {
                // A failing scope must not lengthen the scan cadence for other scopes.
                await Task.Delay(options.Value.Interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessNextScopeAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var scopeProvider = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleScopeProvider>();
        var target = await scopeProvider.GetNextScopeAsync(cancellationToken);
        if (target == null)
            return;

        if (string.IsNullOrWhiteSpace(target.TenantId) || string.IsNullOrWhiteSpace(target.EnvironmentId))
        {
            logger.LogWarning("Credential lifecycle scope provider returned an invalid scope.");
            return;
        }

        var scopeKey = (target.TenantId, target.EnvironmentId);
        var cached = _cursors.GetValueOrDefault(scopeKey);
        if (cached?.FailureTimestamp is long failureTimestamp &&
            timeProvider.GetElapsedTime(failureTimestamp) < cached.RetryDelay)
            return;

        var cursor = cached?.Cursor;
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        using var tenantContext = tenantAccessor.PushContext(new Tenant { Id = target.TenantId, Name = target.TenantId });
        var candidates = scope.ServiceProvider.GetRequiredService<IConnectionDueCandidateStore>();
        try
        {
            var page = await candidates.FindDueCandidatesAsync(
                target.TenantId, target.EnvironmentId, timeProvider.GetUtcNow(), options.Value.BatchSize, cursor, cancellationToken);

            if (page.Items.Count == 0)
            {
                RememberScope(scopeKey, page.NextCursor, 0, null, TimeSpan.Zero);
                return;
            }

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
                RememberFailure(scopeKey, cursor);
                return;
            }

            // Commit the next cursor only after every candidate in the page succeeds.
            RememberScope(scopeKey, page.NextCursor, 0, null, TimeSpan.Zero);
            return;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Credential lifecycle reconciliation scope failed.");
            RememberFailure(scopeKey, cursor);
        }
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
                var reconciliation = await lifecycle.ReconcileAsync(candidate.TenantId, candidate.EnvironmentId,
                    candidate.ConnectionId, cancellationToken);
                // RecoveryRequired with no promotable generation needs operator attention. It is handled
                // for this scan, not a successful recovery, so later pages are not held behind it.
                return reconciliation.Succeeded || reconciliation.SafeErrorCode == "recovery_required";
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

    private void RememberFailure((string TenantId, string EnvironmentId) scopeKey, string? cursor)
    {
        var failures = _cursors.TryGetValue(scopeKey, out var existing)
            ? Math.Min(existing.ConsecutiveFailures + 1, 30)
            : 1;
        RememberScope(scopeKey, cursor, failures, timeProvider.GetTimestamp(), GetRetryDelay(failures));
    }

    private void RememberScope((string TenantId, string EnvironmentId) scopeKey, string? cursor, int consecutiveFailures,
        long? failureTimestamp, TimeSpan retryDelay)
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
        _cursors[scopeKey] = new ScopeCursorEntry(cursor, node, consecutiveFailures, failureTimestamp, retryDelay);
    }

    private TimeSpan GetRetryDelay(int consecutiveFailures)
    {
        var configured = options.Value.RetryBackoff.TotalMilliseconds;
        var multiplier = Math.Pow(2, Math.Min(consecutiveFailures - 1, 20));
        return TimeSpan.FromMilliseconds(Math.Min(configured * multiplier, options.Value.MaxRetryBackoff.TotalMilliseconds));
    }

    private sealed record ScopeCursorEntry(string? Cursor, LinkedListNode<(string TenantId, string EnvironmentId)> Node,
        int ConsecutiveFailures, long? FailureTimestamp, TimeSpan RetryDelay);
}
