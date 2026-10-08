using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Slack.SocketMode.Events;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.SocketMode;

internal sealed record SlackSocketDurableWorkBatch(int RecoveryExamined, int ExecutionAttempts, int Executed,
    int RecoveryAttempts, int Skipped, int TerminalExamined, int WorkflowCleaned, int ReceiptExamined,
    int ReceiptsRemoved, bool RecoveryScanCompleted, bool TerminalScanCompleted, bool ReceiptScansCompleted,
    bool Uncertain);

/// <summary>
/// One serialized, finite discovery/cleanup turn. Notifications merely wake this scanner; they are
/// neither permits nor a queue of execution requests. The host owns scheduling, deadlines and drain.
/// </summary>
internal sealed class SlackSocketDurableWork
{
    private readonly SlackSocketModeConfiguration _configuration;
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly IReadOnlyDictionary<string, SlackSocketSubscription> _subscriptions;
    private readonly SlackSocketReceiptCleanupDriver[] _receipts;
    private readonly int _limit;
    private string? _recoveryCursor;
    private TerminalCursor? _terminalCursor;
    private int _running;

    internal SlackSocketDurableWork(SlackSocketModeConfiguration configuration, IServiceScopeFactory scopes, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _scopes = scopes;
        _time = timeProvider;
        _limit = Math.Min(configuration.Limits.MaximumPendingEnvelopes, 1000);
        _subscriptions = configuration.Subscriptions.ToDictionary(x => x.Configuration.Id, StringComparer.Ordinal);
        _receipts = configuration.Subscriptions.Select(x => x.Configuration.Policy.CleanupAuthority)
            .Distinct(StringComparer.Ordinal).Select(authority => new SlackSocketReceiptCleanupDriver(scopes, authority)).ToArray();
    }

    internal async Task<SlackSocketDurableWorkBatch> RunBatchAsync(SlackSocketOperation operation)
    {
        var cancellationToken = operation.Token;
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException("socket_durable_work_already_running");
        }
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            using var tenant = services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
            {
                Id = _configuration.TenantId, Name = _configuration.TenantId
            });
            AdmissionElsaDbContext? terminalDb = null;
            try
            {
                var store = services.GetRequiredService<IAdmissionStore>();
                var execution = services.GetRequiredService<AdmissionExecutionService>();
                var owners = services.GetRequiredService<AdmissionAuthorityRegistry>();
                var examined = 0;
                var executionAttempts = 0;
                var executed = 0;
                var recoveryAttempts = 0;
                var skipped = 0;
                var uncertain = false;
                var recoveryCompleted = false;
                try
                {
                    var page = await execution.ListRecoveryAsync(_limit, _recoveryCursor, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (page.Items.Count > _limit)
                    {
                        throw new InvalidOperationException("socket_recovery_page_exceeded");
                    }
                    foreach (var item in page.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        examined++;
                        try
                        {
                            var record = await store.FindAsync(item.AdmissionId, cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                            if (record == null || !MatchesBinding(record) ||
                                (record.WorkflowInstanceId != null && owners.HasOwner(record.WorkflowInstanceId)) ||
                                record.State is not (AdmissionState.Admitted or AdmissionState.Materialized or AdmissionState.Creating or AdmissionState.StartPreparing or AdmissionState.StartAuthorized))
                            {
                                skipped++;
                            }
                            else if (!await HasCurrentBindingAsync(store, record.SubscriptionId,
                                record.State is AdmissionState.Admitted or AdmissionState.Materialized, cancellationToken))
                            {
                                skipped++;
                            }
                            else if (record.State is AdmissionState.Admitted or AdmissionState.Materialized)
                            {
                                ValidateHumanPayload(record);
                                executionAttempts++;
                                if (await execution.ExecuteAsync(record.Id, cancellationToken) != null)
                                {
                                    executed++;
                                }
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                            else
                            {
                                // Recover only classifies an unknown boundary; it never resumes, inserts,
                                // resolves or recreates a capability. It rechecks live owners itself.
                                recoveryAttempts++;
                                await execution.RecoverAsync(record.Id, cancellationToken);
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            // Advance across a failed row instead of starving all later admissions.
                            // A completed scan revisits it; actual state/one-shot guards remain authoritative.
                            uncertain = true;
                        }
                        _recoveryCursor = item.AdmissionId;
                    }
                    recoveryCompleted = page.AfterId == null;
                    if (recoveryCompleted)
                    {
                        _recoveryCursor = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    uncertain = true;
                }

                var terminalExamined = 0;
                var cleaned = 0;
                var terminalCompleted = false;
                try
                {
                    terminalDb = await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync(cancellationToken);
                    var page = await SelectTerminalAsync(services, terminalDb, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var candidate in page)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        terminalExamined++;
                        // Terminal cleanup belongs to the stable configured namespace, not a
                        // current execution epoch. The store applies the historical admitted policy;
                        // only the currently approved authority is supplied, never an adopted old one.
                        if (_subscriptions.TryGetValue(candidate.SubscriptionId, out var captured) &&
                            (candidate.WorkflowInstanceId == null || !owners.HasOwner(candidate.WorkflowInstanceId)))
                        {
                            if (await store.CleanupAsync(candidate.Id, candidate.Revision, captured.Configuration.Policy.CleanupAuthority,
                                _time.GetUtcNow(), cancellationToken))
                            {
                                cleaned++;
                            }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        // A stale/ineligible row must not pin discovery. Unknown cleanup preserves
                        // this candidate's position and reports uncertainty, never a confirmed removal.
                        _terminalCursor = new(candidate.TerminalAt, candidate.Id);
                    }
                    terminalCompleted = page.Count < _limit;
                    if (terminalCompleted)
                    {
                        _terminalCursor = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    uncertain = true;
                }

                var receiptExamined = 0;
                var receiptsRemoved = 0;
                var receiptCompleted = true;
                foreach (var driver in _receipts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        // Every authority shares this retained, fixed-tenant scope until callbacks settle.
                        var batch = await driver.RunBatchAsync(services.GetRequiredService<ISlackSocketDiscardStore>(), _limit, _time.GetUtcNow(), cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        receiptExamined = SaturatingAdd(receiptExamined, batch.Examined);
                        receiptsRemoved = SaturatingAdd(receiptsRemoved, batch.Removed);
                        receiptCompleted &= batch.ScanCompleted;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        uncertain = true;
                        receiptCompleted = false;
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                return new(examined, executionAttempts, executed, recoveryAttempts, skipped, terminalExamined, cleaned,
                    receiptExamined, receiptsRemoved, recoveryCompleted, terminalCompleted, receiptCompleted, uncertain);
            }
            finally
            {
                // The host operation keeps its private deadline alive through this callback
                // join AND subsequent async scope disposal. It owns any incomplete unwind.
                try
                {
                    await operation.CloseBodyAsync();
                }
                finally
                {
                    if (terminalDb != null)
                    {
                        await terminalDb.DisposeAsync();
                    }
                }
            }
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private bool MatchesBinding(AdmissionRecord record) => _subscriptions.TryGetValue(record.SubscriptionId, out var captured) &&
        record.ConfigurationFingerprint == captured.Configuration.ConfigurationFingerprint &&
        record.ActivationEpoch == captured.ActivationEpoch &&
        record.AdmittedConfigurationJson == JsonSerializer.Serialize(captured.Configuration);

    private void ValidateHumanPayload(AdmissionRecord record)
    {
        var payload = record.Payload ?? throw new InvalidOperationException("socket_durable_payload_unavailable");
        var message = SlackSocketEventPayload.DeserializeValidatedHuman(payload);
        var expected = new AdmissionEvent(record.SubscriptionId, _configuration.InstallationId, _configuration.ChannelId,
            record.ProviderEventId!, record.EventOccurredAt, true, false, payload);
        if (message.BindingFingerprint != _configuration.BindingFingerprint || message.AppId != _configuration.ExpectedAppId ||
            message.TeamId != _configuration.ExpectedTeamId || message.EnterpriseId != _configuration.ExpectedEnterpriseId ||
            message.SelfUserId != _configuration.SelfUserId || message.ChannelId != _configuration.ChannelId ||
            message.ProviderEventId != record.ProviderEventId || message.OccurredAt != record.EventOccurredAt ||
            record.PayloadFingerprint != AdmissionHash.Compute(payload) || record.EventFingerprint != AdmissionEventFingerprint.Compute(expected))
        {
            // Leave the original record untouched for explicit reconciliation; a different
            // listener binding cannot reinterpret historical input or manufacture authority.
            throw new InvalidOperationException("socket_durable_payload_binding_invalid");
        }
    }

    private async Task<bool> HasCurrentBindingAsync(IAdmissionStore store, string subscriptionId, bool requireActive, CancellationToken cancellationToken)
    {
        var captured = _subscriptions[subscriptionId];
        var current = await store.FindSubscriptionAsync(subscriptionId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        // Withdrawal blocks new starts, but conservative classification of a genuinely
        // unknown prior boundary remains effect-free within the same captured binding.
        return current != null && (!requireActive || current is { Active: true, Retired: false, BootstrapVerified: true, ReconciliationCode: null }) &&
            current.ConfigurationFingerprint == captured.Configuration.ConfigurationFingerprint && current.ActivationEpoch == captured.ActivationEpoch &&
            current.ConfigurationJson == JsonSerializer.Serialize(captured.Configuration);
    }

    private async Task<IReadOnlyList<TerminalCandidate>> SelectTerminalAsync(IServiceProvider services, AdmissionElsaDbContext db, CancellationToken cancellationToken)
    {
        // The Socket host attests this selected factory before scheduling work. This is ONLY
        // read-only discovery; all mutation/capacity/retention decisions remain in CleanupAsync.
        if (services.GetRequiredService<AdmissionPersistenceScope>() != new AdmissionPersistenceScope(_configuration.TenantId, _configuration.EnvironmentId))
        {
            throw new InvalidOperationException("socket_cleanup_scope_invalid");
        }
        SlackSocketModeHostValidator.DemandDatabaseLayout(db, Elsa.Persistence.EFCore.ElsaDbContextBase.MigrationsHistoryTable);
        if (db.GetType() != typeof(AdmissionElsaDbContext) || db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" ||
            !db.IsTenantFilteringEnabled || db.TenantId != _configuration.TenantId || db.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException("socket_cleanup_provider_invalid");
        }
        var subscriptions = _subscriptions.Keys.ToArray();
        var query = db.Admissions.AsNoTracking().Where(x =>
            EF.Property<string>(x, "TenantId") == _configuration.TenantId && EF.Property<string>(x, "EnvironmentId") == _configuration.EnvironmentId &&
            subscriptions.Contains(x.SubscriptionId) && x.State == AdmissionState.Terminal && x.TerminalAt != null &&
            x.TerminalDisposition != null && !x.AuthorityOutstanding && x.ActiveReservationReleased && !x.RetainedRecordReleased &&
            (x.Payload != null || x.IdentityHash != null || x.ProviderEventId != null));
        if (_terminalCursor is { } cursor)
        {
            query = query.Where(x => x.TerminalAt > cursor.TerminalAt ||
                (x.TerminalAt == cursor.TerminalAt && string.Compare(x.Id, cursor.Id) > 0));
        }
        return await query.OrderBy(x => x.TerminalAt).ThenBy(x => x.Id).Take(_limit)
            .Select(x => new TerminalCandidate(x.Id, x.Revision, x.SubscriptionId,
                x.TerminalAt!.Value, x.WorkflowInstanceId)).ToListAsync(cancellationToken);
    }

    private static int SaturatingAdd(int left, int right) => (int)Math.Min(int.MaxValue, (long)left + right);
    private sealed record TerminalCursor(DateTimeOffset TerminalAt, string Id);
    private sealed record TerminalCandidate(string Id, long Revision, string SubscriptionId,
        DateTimeOffset TerminalAt, string? WorkflowInstanceId);
}
