using System.Text;
using System.Text.Json;
using Elsa.Workflows.Admission;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Admission.Persistence.EFCore;

/// <summary>
/// Subscription-serialized ledger transactions. The selected provider lock includes absent subscriptions.
/// No retry strategy or commit readback is used: any uncertain commit propagates to the caller.
/// </summary>
public sealed class EFCoreAdmissionStore(
    IDbContextFactory<AdmissionElsaDbContext> factory,
    AdmissionPersistenceScope scope,
    IAdmissionTransactionLock transactionLock) : IAdmissionStore
{
    public async Task<AdmissionSubscription> ProvisionAsync(AdmissionSubscriptionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);
        await using var db = await CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await transactionLock.AcquireAsync(db, configuration.Id, cancellationToken);
        var existing = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == configuration.Id, cancellationToken);
        if (existing != null)
        {
            DemandScope(db, existing);
            if (existing.Retired || existing.ConfigurationFingerprint != configuration.ConfigurationFingerprint)
            {
                throw new InvalidOperationException("admission_subscription_configuration_conflict");
            }
            return existing;
        }

        var subscription = new AdmissionSubscription
        {
            Id = configuration.Id, ConfigurationJson = JsonSerializer.Serialize(configuration),
            ConfigurationFingerprint = configuration.ConfigurationFingerprint, Revision = 1
        };
        db.Subscriptions.Add(subscription);
        StampScope(db, subscription);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return subscription;
    }

    public Task<AdmissionSubscription?> ReconfigureAsync(AdmissionSubscriptionConfiguration configuration, long revision, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);
        return MutateSubscriptionAsync(configuration.Id, revision, subscription =>
        {
            var current = subscription.Configuration;
            if (subscription.Active || subscription.Retired || current.TenantId != configuration.TenantId ||
                current.EnvironmentId != configuration.EnvironmentId || current.InstallationId != configuration.InstallationId ||
                current.ActivationBoundary != configuration.ActivationBoundary)
            {
                return false;
            }
            subscription.ConfigurationJson = JsonSerializer.Serialize(configuration);
            subscription.ConfigurationFingerprint = configuration.ConfigurationFingerprint;
            subscription.BootstrapVerified = false;
            return true;
        }, cancellationToken);
    }

    public async Task<AdmissionSubscription?> FindSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAsync(cancellationToken);
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
        if (subscription != null)
        {
            DemandScope(db, subscription);
        }
        return subscription;
    }

    public Task<AdmissionSubscription?> VerifyBootstrapAsync(string subscriptionId, long revision, string configurationFingerprint, CancellationToken cancellationToken = default) =>
        MutateSubscriptionAsync(subscriptionId, revision, subscription =>
        {
            if (subscription.Active || subscription.Retired || subscription.ReconciliationCode != null ||
                subscription.ConfigurationFingerprint != configurationFingerprint)
            {
                return false;
            }
            subscription.Configuration.Validate();
            subscription.BootstrapVerified = true;
            return true;
        }, cancellationToken);

    public Task<AdmissionSubscription?> ActivateAsync(string subscriptionId, long revision, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        MutateSubscriptionAsync(subscriptionId, revision, subscription =>
        {
            if (subscription.Active || subscription.Retired || !subscription.BootstrapVerified || subscription.ReconciliationCode != null || subscription.Configuration.ActivationBoundary > now.ToUniversalTime())
            {
                return false;
            }
            subscription.Configuration.Validate();
            subscription.Active = true;
            subscription.ActivationEpoch = checked(subscription.ActivationEpoch + 1);
            return true;
        }, cancellationToken);

    public Task<AdmissionSubscription?> WithdrawAsync(string subscriptionId, long revision, bool retire, string? reconciliationCode, CancellationToken cancellationToken = default)
    {
        if (reconciliationCode != null)
        {
            DemandCode(reconciliationCode);
        }
        return MutateSubscriptionAsync(subscriptionId, revision, subscription =>
        {
            subscription.Active = false;
            subscription.Retired |= retire;
            subscription.ReconciliationCode = reconciliationCode;
            return true;
        }, cancellationToken);
    }

    public async Task<AdmissionResult> AdmitAsync(AdmissionEvent message, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (message.Payload == null || message.ProviderEventId == null ||
            Encoding.UTF8.GetByteCount(message.Payload) > AdmissionLimits.PayloadBytes ||
            Encoding.UTF8.GetByteCount(message.ProviderEventId) > AdmissionLimits.ProviderEventIdBytes)
        {
            return new(AdmissionOutcome.Rejected, null, null);
        }
        await using var db = await CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await transactionLock.AcquireAsync(db, message.SubscriptionId, cancellationToken);
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == message.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            return new(AdmissionOutcome.Inactive, null, null);
        }
        DemandScope(db, subscription);
        var configuration = subscription.Configuration;
        ValidateConfiguration(configuration);
        if (message.InstallationId != configuration.InstallationId || message.ChannelId != configuration.ChannelId)
        {
            return new(AdmissionOutcome.Quarantined, null, null);
        }
        if (!message.IsHumanMessage || message.IsLoopMessage)
        {
            return new(AdmissionOutcome.Filtered, null, null);
        }
        if (string.IsNullOrWhiteSpace(message.ProviderEventId) || message.ProviderEventId.Length > 256 ||
            Encoding.UTF8.GetByteCount(message.ProviderEventId) > configuration.Policy.MaximumProviderEventIdBytes ||
            Encoding.UTF8.GetByteCount(message.Payload) > configuration.Policy.MaximumPayloadBytes || message.OccurredAt == null)
        {
            return Rejected(configuration.Policy.InvalidEventDisposition);
        }
        var identity = AdmissionHash.Identity(configuration, message.ProviderEventId);
        var duplicate = await db.Admissions.SingleOrDefaultAsync(x => x.IdentityHash == identity, cancellationToken);
        if (duplicate != null)
        {
            DemandScope(db, duplicate);
            return duplicate.ConfigurationFingerprint == subscription.ConfigurationFingerprint && duplicate.EventFingerprint == AdmissionEventFingerprint.Compute(message)
                ? new(AdmissionOutcome.Duplicate, duplicate.Id, duplicate.Revision)
                : new(AdmissionOutcome.Quarantined, duplicate.Id, duplicate.Revision);
        }
        if (!subscription.Active || subscription.Retired || subscription.ReconciliationCode != null)
        {
            return new(AdmissionOutcome.Inactive, null, null);
        }
        var occurredAt = message.OccurredAt.Value.ToUniversalTime();
        now = now.ToUniversalTime();
        if (occurredAt > now + configuration.Policy.MaximumClockSkew)
        {
            return Rejected(configuration.Policy.InvalidEventDisposition);
        }
        if (occurredAt < configuration.ActivationBoundary || occurredAt < now - configuration.Policy.MaximumEventAge)
        {
            return Rejected(configuration.Policy.LateEventDisposition);
        }
        if (subscription.ActiveReservations >= configuration.Policy.ActiveCapacity || subscription.RetainedRecords >= configuration.Policy.RetainedRecordCapacity)
        {
            return new(AdmissionOutcome.CapacityExceeded, null, null);
        }
        var record = new AdmissionRecord
        {
            Id = Guid.NewGuid().ToString("N"), SubscriptionId = subscription.Id, IdentityHash = identity,
            ProviderEventId = message.ProviderEventId, Payload = message.Payload, ConfigurationFingerprint = subscription.ConfigurationFingerprint,
            ActivationEpoch = subscription.ActivationEpoch, AdmittedConfigurationJson = subscription.ConfigurationJson,
            PayloadFingerprint = AdmissionHash.Compute(message.Payload), EventFingerprint = AdmissionEventFingerprint.Compute(message), AdmittedAt = now, EventOccurredAt = occurredAt, Revision = 1, State = AdmissionState.Admitted
        };
        db.Admissions.Add(record);
        StampScope(db, record);
        subscription.ActiveReservations = checked(subscription.ActiveReservations + 1);
        subscription.RetainedRecords = checked(subscription.RetainedRecords + 1);
        subscription.Revision = checked(subscription.Revision + 1);
        await db.SaveChangesAsync(cancellationToken);
        // A lost commit response is not an acknowledgement, even if another process later sees this row.
        await transaction.CommitAsync(cancellationToken);
        return new(AdmissionOutcome.Committed, record.Id, record.Revision);
    }

    public async Task<AdmissionRecord?> FindAsync(string admissionId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAsync(cancellationToken);
        var record = await db.Admissions.SingleOrDefaultAsync(x => x.Id == admissionId, cancellationToken);
        if (record != null)
        {
            DemandScope(db, record);
        }
        return record;
    }

    public async Task<AdmissionRecord?> FindByInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAsync(cancellationToken);
        // Deliberately global: a foreign-scope owned ID must fail closed, not appear unowned.
        var record = await db.Admissions.SingleOrDefaultAsync(x => x.WorkflowInstanceId == instanceId, cancellationToken);
        if (record != null)
        {
            DemandScope(db, record);
        }
        return record;
    }

    public async Task<IReadOnlyList<AdmissionRecord>> FindRecoverableAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        await using var db = await CreateAsync(cancellationToken);
        return await db.Admissions.AsNoTracking()
            .Where(x => EF.Property<string>(x, "TenantId") == scope.TenantId && EF.Property<string>(x, "EnvironmentId") == scope.EnvironmentId &&
                x.State != AdmissionState.Terminal)
            .OrderBy(x => x.State).ThenBy(x => x.AdmittedAt).ThenBy(x => x.Id).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<AdmissionRecord?> BeginCreationAsync(string admissionId, long revision, string instanceId, CancellationToken cancellationToken = default)
    {
        DemandCode(instanceId);
        return MutateRecordAsync(admissionId, revision, (record, subscription) =>
        {
            if (record.State != AdmissionState.Admitted || record.WorkflowInstanceId != null || !ActiveBinding(record, subscription))
            {
                return false;
            }
            record.WorkflowInstanceId = instanceId;
            record.State = AdmissionState.Creating;
            return true;
        }, cancellationToken);
    }

    public Task<AdmissionRecord?> CompleteCreationAsync(string admissionId, long revision, CancellationToken cancellationToken = default) =>
        MutateRecordAsync(admissionId, revision, (record, _) =>
        {
            if (record.State != AdmissionState.Creating)
            {
                return false;
            }
            record.State = AdmissionState.Materialized;
            return true;
        }, cancellationToken);

    public Task<AdmissionRecord?> PrepareStartAsync(string admissionId, long revision, string attemptId, string? checkpointFingerprint, string? bookmarkId, CancellationToken cancellationToken = default)
    {
        DemandCode(attemptId);
        return MutateRecordAsync(admissionId, revision, (record, subscription) =>
        {
            if (!ActiveBinding(record, subscription) || record.AuthorityOutstanding)
            {
                return false;
            }
            var initial = record.State == AdmissionState.Materialized && checkpointFingerprint == null && bookmarkId == null && record.AttemptId == null;
            var continuation = record.State == AdmissionState.ExecutionObserved && checkpointFingerprint != null && bookmarkId != null &&
                record.CheckpointFingerprint == checkpointFingerprint && ReadBookmarks(record).Contains(bookmarkId, StringComparer.Ordinal);
            if (!initial && !continuation)
            {
                return false;
            }
            record.AttemptId = attemptId;
            record.State = AdmissionState.StartPreparing;
            return true;
        }, cancellationToken);
    }

    public Task<AdmissionRecord?> AuthorizeStartAsync(string admissionId, long revision, string attemptId, CancellationToken cancellationToken = default) =>
        MutateRecordAsync(admissionId, revision, (record, subscription) =>
        {
            if (record.State != AdmissionState.StartPreparing || record.AttemptId != attemptId || record.AuthorityOutstanding || !ActiveBinding(record, subscription))
            {
                return false;
            }
            record.State = AdmissionState.StartAuthorized;
            record.AuthorityOutstanding = true;
            record.CheckpointFingerprint = null;
            record.BookmarkIdsJson = null;
            return true;
        }, cancellationToken);

    public Task<AdmissionRecord?> CompleteExecutionAsync(string admissionId, long revision, string attemptId, string checkpointFingerprint,
        IReadOnlyCollection<string> bookmarkIds, bool durablyCompleted, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        DemandFingerprint(checkpointFingerprint);
        if (bookmarkIds.Count > 10000 || bookmarkIds.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 256) || bookmarkIds.Distinct(StringComparer.Ordinal).Count() != bookmarkIds.Count ||
            durablyCompleted && bookmarkIds.Count != 0)
        {
            throw new ArgumentException("admission_checkpoint_bookmarks_invalid");
        }
        return MutateRecordAsync(admissionId, revision, (record, subscription) =>
        {
            if (record.State != AdmissionState.StartAuthorized || record.AttemptId != attemptId || !record.AuthorityOutstanding)
            {
                return false;
            }
            record.AuthorityOutstanding = false;
            record.CheckpointFingerprint = checkpointFingerprint;
            record.BookmarkIdsJson = JsonSerializer.Serialize(bookmarkIds.OrderBy(x => x, StringComparer.Ordinal));
            record.State = AdmissionState.ExecutionObserved;
            if (durablyCompleted)
            {
                Terminal(record, subscription, AdmissionTerminalDisposition.Completed, now);
            }
            return true;
        }, cancellationToken);
    }

    public Task<AdmissionRecord?> RequireRecoveryAsync(string admissionId, long revision, string recoveryCode, CancellationToken cancellationToken = default)
    {
        DemandCode(recoveryCode);
        return MutateRecordAsync(admissionId, revision, (record, _) =>
        {
            if (record.State == AdmissionState.Terminal)
            {
                return false;
            }
            record.State = AdmissionState.RecoveryRequired;
            record.RecoveryCode = recoveryCode;
            return true;
        }, cancellationToken);
    }

    public Task<AdmissionRecord?> ResolveAsync(string admissionId, long revision, AdmissionTerminalDisposition disposition,
        string auditReference, bool establishedOwnerQuiescence, bool noUnknownEffects, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        DemandCode(auditReference);
        return MutateRecordAsync(admissionId, revision, (record, subscription) =>
        {
            if (record.State == AdmissionState.Terminal || disposition == AdmissionTerminalDisposition.Completed || !Enum.IsDefined(disposition))
            {
                return false;
            }
            if (disposition == AdmissionTerminalDisposition.SuppressedBeforeStart &&
                (record.State is not (AdmissionState.Admitted or AdmissionState.Materialized) || record.AttemptId != null || record.AuthorityOutstanding || record.RecoveryCode != null))
            {
                return false;
            }
            if (disposition == AdmissionTerminalDisposition.Resolved && (!establishedOwnerQuiescence || !noUnknownEffects))
            {
                return false;
            }
            record.AuthorityOutstanding = false;
            record.AuditReference = auditReference;
            Terminal(record, subscription, disposition, now);
            return true;
        }, cancellationToken);
    }

    public async Task<bool> CleanupAsync(string admissionId, long revision, string cleanupAuthority, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var record = await LoadLockedRecordAsync(db, admissionId, cancellationToken);
        if (record == null || record.Revision != revision || record.State != AdmissionState.Terminal || record.TerminalAt == null ||
            record.TerminalDisposition == null || record.AuthorityOutstanding || !record.ActiveReservationReleased || record.RetainedRecordReleased)
        {
            return false;
        }
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == record.SubscriptionId, cancellationToken);
        DemandScope(db, subscription);
        var policy = JsonSerializer.Deserialize<AdmissionSubscriptionConfiguration>(record.AdmittedConfigurationJson)!.Policy;
        policy.Validate();
        if (cleanupAuthority != policy.CleanupAuthority || now.ToUniversalTime() < record.TerminalAt.Value + policy.PayloadRetention)
        {
            return false;
        }
        var changed = record.Payload != null;
        record.Payload = null;
        if (now.ToUniversalTime() >= record.TerminalAt.Value + policy.IdentityHorizon)
        {
            changed |= record.IdentityHash != null || record.ProviderEventId != null;
            record.IdentityHash = null;
            record.ProviderEventId = null;
            if (record.WorkflowInstanceId == null)
            {
                if (subscription.RetainedRecords <= 0)
                {
                    throw new InvalidOperationException("admission_capacity_invariant_failed");
                }
                record.RetainedRecordReleased = true;
                subscription.RetainedRecords--;
                db.Admissions.Remove(record);
                changed = true;
            }
        }
        if (!changed)
        {
            return false;
        }
        record.Revision = checked(record.Revision + 1);
        subscription.Revision = checked(subscription.Revision + 1);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<AdmissionSubscription?> MutateSubscriptionAsync(string id, long revision, Func<AdmissionSubscription, bool> update, CancellationToken cancellationToken)
    {
        await using var db = await CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await transactionLock.AcquireAsync(db, id, cancellationToken);
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription == null)
        {
            return null;
        }
        DemandScope(db, subscription);
        if (subscription.Revision != revision || !update(subscription))
        {
            return null;
        }
        subscription.Revision = checked(subscription.Revision + 1);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return subscription;
    }

    private async Task<AdmissionRecord?> MutateRecordAsync(string id, long revision, Func<AdmissionRecord, AdmissionSubscription, bool> update, CancellationToken cancellationToken)
    {
        await using var db = await CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var record = await LoadLockedRecordAsync(db, id, cancellationToken);
        if (record == null || record.Revision != revision)
        {
            return null;
        }
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == record.SubscriptionId, cancellationToken);
        DemandScope(db, subscription);
        if (!update(record, subscription))
        {
            return null;
        }
        record.Revision = checked(record.Revision + 1);
        subscription.Revision = checked(subscription.Revision + 1);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return record;
    }

    private async Task<AdmissionRecord?> LoadLockedRecordAsync(AdmissionElsaDbContext db, string id, CancellationToken cancellationToken)
    {
        var subscriptionId = await db.Admissions.Where(x => x.Id == id).Select(x => x.SubscriptionId).SingleOrDefaultAsync(cancellationToken);
        if (subscriptionId == null)
        {
            return null;
        }
        await transactionLock.AcquireAsync(db, subscriptionId, cancellationToken);
        var record = await db.Admissions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record != null)
        {
            DemandScope(db, record);
        }
        return record;
    }

    private async Task<AdmissionElsaDbContext> CreateAsync(CancellationToken cancellationToken)
    {
        scope.Validate();
        var db = await factory.CreateDbContextAsync(cancellationToken);
        if (db.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            await db.DisposeAsync();
            throw new InvalidOperationException("admission_retrying_execution_strategy_unsupported");
        }
        return db;
    }

    private void ValidateConfiguration(AdmissionSubscriptionConfiguration configuration)
    {
        configuration.Validate();
        if (configuration.TenantId != scope.TenantId || configuration.EnvironmentId != scope.EnvironmentId)
        {
            throw new InvalidOperationException("admission_trusted_scope_conflict");
        }
    }

    private void StampScope(AdmissionElsaDbContext db, object entity)
    {
        db.Entry(entity).Property("TenantId").CurrentValue = scope.TenantId;
        db.Entry(entity).Property("EnvironmentId").CurrentValue = scope.EnvironmentId;
    }

    private void DemandScope(AdmissionElsaDbContext db, object entity)
    {
        if ((string?)db.Entry(entity).Property("TenantId").CurrentValue != scope.TenantId ||
            (string?)db.Entry(entity).Property("EnvironmentId").CurrentValue != scope.EnvironmentId)
        {
            throw new InvalidOperationException("admission_trusted_scope_conflict");
        }
    }

    private static bool ActiveBinding(AdmissionRecord record, AdmissionSubscription subscription) =>
        subscription.Active && !subscription.Retired && subscription.ReconciliationCode == null && subscription.BootstrapVerified &&
        record.ConfigurationFingerprint == subscription.ConfigurationFingerprint && record.ActivationEpoch == subscription.ActivationEpoch;

    private static string[] ReadBookmarks(AdmissionRecord record) => record.BookmarkIdsJson == null ? [] : JsonSerializer.Deserialize<string[]>(record.BookmarkIdsJson)!;
    private static AdmissionResult Rejected(AdmissionRejectedEventDisposition disposition) => new(disposition == AdmissionRejectedEventDisposition.Quarantine ? AdmissionOutcome.Quarantined : AdmissionOutcome.Rejected, null, null);

    private static void Terminal(AdmissionRecord record, AdmissionSubscription subscription, AdmissionTerminalDisposition disposition, DateTimeOffset now)
    {
        if (record.ActiveReservationReleased || subscription.ActiveReservations <= 0)
        {
            throw new InvalidOperationException("admission_capacity_invariant_failed");
        }
        record.State = AdmissionState.Terminal;
        record.TerminalDisposition = disposition;
        record.TerminalAt = now.ToUniversalTime();
        record.ActiveReservationReleased = true;
        subscription.ActiveReservations--;
    }

    private static void DemandCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(x => !char.IsAsciiLetterOrDigit(x) && x is not ('-' or '_' or ':' or '.' or '/')))
        {
            throw new ArgumentException("admission_sanitized_reference_required");
        }
    }

    private static void DemandFingerprint(string value)
    {
        if (value.Length != 64 || !value.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("admission_sha256_required");
        }
    }
}
