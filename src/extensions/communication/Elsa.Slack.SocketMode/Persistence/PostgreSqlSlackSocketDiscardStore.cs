using System.Text;
using System.Text.Json;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Slack.SocketMode.Persistence;

/// <summary>Subscription-serialized final receipts. An uncertain commit always propagates.</summary>
internal sealed class PostgreSqlSlackSocketDiscardStore(IDbContextFactory<AdmissionElsaDbContext> admissionFactory,
    AdmissionPersistenceScope scope, IAdmissionTransactionLock transactionLock, SlackSocketReceiptTransactions transactions) : ISlackSocketDiscardStore
{
    public async Task ValidateProvisioningAsync(CancellationToken cancellationToken = default)
    {
        await using var admission = await CreateAsync(cancellationToken);
        await using var transaction = await admission.Database.BeginTransactionAsync(cancellationToken);
        await using var receipts = await transactions.EnlistAsync(admission, cancellationToken);
        // Actual queries and migration history, not configuration-only assertions.
        _ = await admission.Subscriptions.AnyAsync(cancellationToken);
        _ = await receipts.Receipts.AnyAsync(cancellationToken);
        var known = receipts.Database.GetMigrations().ToArray();
        var applied = (await receipts.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var admissionKnown = admission.Database.GetMigrations().ToArray();
        var admissionApplied = (await admission.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        if (known.Length != 1 || known[0] != "20261008110000_InitialSlackSocketReceipts" || !known.All(applied.Contains) ||
            admissionKnown.Length != 1 || admissionKnown[0] != "20261008040000_InitialAdmission" || !admissionKnown.All(admissionApplied.Contains) ||
            receipts.Database.HasPendingModelChanges() || admission.Database.HasPendingModelChanges())
        {
            throw new InvalidOperationException("socket_receipt_schema_not_provisioned");
        }
    }

    public async Task<SlackSocketDiscardResult> RecordDiscardAsync(SlackSocketDiscardRequest request, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = request.Event;
        if (message == null || !Bounded(message.SubscriptionId, 256) || !Bounded(message.InstallationId, 256) || !Bounded(message.ChannelId, 256) ||
            !Bounded(message.ProviderEventId, AdmissionLimits.ProviderEventIdBytes) || message.Payload == null ||
            Encoding.UTF8.GetByteCount(message.Payload) > AdmissionLimits.PayloadBytes || message.OccurredAt == null ||
            !Enum.IsDefined(request.Reason) || !Fingerprint(request.BindingFingerprint) || !Fingerprint(request.ExpectedConfigurationFingerprint) ||
            request.ExpectedActivationEpoch <= 0 || message.IsHumanMessage)
        {
            return Result(SlackSocketDiscardOutcome.Rejected);
        }
        await using var admission = await CreateAsync(cancellationToken);
        await using var transaction = await admission.Database.BeginTransactionAsync(cancellationToken);
        await transactionLock.AcquireAsync(admission, message.SubscriptionId, cancellationToken);
        var subscription = await admission.Subscriptions.SingleOrDefaultAsync(x => x.Id == message.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            return Result(SlackSocketDiscardOutcome.Inactive);
        }
        DemandScope(admission, subscription);
        var configuration = subscription.Configuration;
        configuration.Validate();
        if (configuration.TenantId != scope.TenantId || configuration.EnvironmentId != scope.EnvironmentId ||
            message.InstallationId != configuration.InstallationId || message.ChannelId != configuration.ChannelId ||
            request.ExpectedConfigurationFingerprint != subscription.ConfigurationFingerprint || request.ExpectedActivationEpoch != subscription.ActivationEpoch)
        {
            return Result(SlackSocketDiscardOutcome.Quarantined);
        }
        if (!subscription.Active || subscription.Retired || !subscription.BootstrapVerified || subscription.ReconciliationCode != null)
        {
            return Result(SlackSocketDiscardOutcome.Inactive);
        }
        if (Encoding.UTF8.GetByteCount(message.ProviderEventId) > configuration.Policy.MaximumProviderEventIdBytes ||
            Encoding.UTF8.GetByteCount(message.Payload) > configuration.Policy.MaximumPayloadBytes)
        {
            return Rejected(configuration.Policy.InvalidEventDisposition);
        }
        await using var receipts = await transactions.EnlistAsync(admission, cancellationToken);
        var identity = AdmissionHash.Identity(configuration, message.ProviderEventId);
        // Check both kinds before accepting any duplicate, including corrupted dual-table history.
        if (await admission.Admissions.AnyAsync(x => x.IdentityHash == identity, cancellationToken))
        {
            return Result(SlackSocketDiscardOutcome.Quarantined);
        }
        var duplicate = await receipts.Receipts.SingleOrDefaultAsync(x => x.IdentityHash == identity, cancellationToken);
        var eventFingerprint = AdmissionEventFingerprint.Compute(message);
        if (duplicate != null)
        {
            DemandScope(duplicate);
            return duplicate.ConfigurationFingerprint == subscription.ConfigurationFingerprint && duplicate.ActivationEpoch == subscription.ActivationEpoch &&
                duplicate.BindingFingerprint == request.BindingFingerprint && duplicate.Reason == request.Reason && duplicate.EventFingerprint == eventFingerprint
                ? new(SlackSocketDiscardOutcome.Duplicate, duplicate.Id, duplicate.Revision)
                : Result(SlackSocketDiscardOutcome.Quarantined);
        }
        now = now.ToUniversalTime();
        var occurredAt = message.OccurredAt.Value.ToUniversalTime();
        if (occurredAt > now && occurredAt - now > configuration.Policy.MaximumClockSkew)
        {
            return Rejected(configuration.Policy.InvalidEventDisposition);
        }
        if (occurredAt < configuration.ActivationBoundary || occurredAt < now && now - occurredAt > configuration.Policy.MaximumEventAge)
        {
            return Rejected(configuration.Policy.LateEventDisposition);
        }
        if (subscription.RetainedRecords >= configuration.Policy.RetainedRecordCapacity)
        {
            return Result(SlackSocketDiscardOutcome.CapacityExceeded);
        }
        var receipt = new SlackSocketDiscardReceipt
        {
            Id = Guid.NewGuid().ToString("N"), SubscriptionId = subscription.Id, TenantId = scope.TenantId, EnvironmentId = scope.EnvironmentId,
            IdentityHash = identity, ProviderEventId = message.ProviderEventId, Reason = request.Reason, BindingFingerprint = request.BindingFingerprint,
            ConfigurationFingerprint = subscription.ConfigurationFingerprint, ConfigurationJson = subscription.ConfigurationJson, ActivationEpoch = subscription.ActivationEpoch,
            PayloadFingerprint = AdmissionHash.Compute(message.Payload), EventFingerprint = eventFingerprint, EventOccurredAt = occurredAt,
            DecisionAt = RoundUp(now), Revision = 1
        };
        receipts.Receipts.Add(receipt);
        subscription.RetainedRecords = checked(subscription.RetainedRecords + 1);
        subscription.Revision = checked(subscription.Revision + 1);
        await receipts.SaveChangesAsync(cancellationToken);
        await admission.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(SlackSocketDiscardOutcome.Committed, receipt.Id, receipt.Revision);
    }

    public async Task<IReadOnlyList<SlackSocketDiscardCleanupCandidate>> FindCleanupCandidatesAsync(int limit, string? afterId, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000 || afterId != null && !Bounded(afterId, 256))
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        await using var admission = await CreateAsync(cancellationToken);
        await using var transaction = await admission.Database.BeginTransactionAsync(cancellationToken);
        await using var receipts = await transactions.EnlistAsync(admission, cancellationToken);
        return await receipts.Receipts.AsNoTracking().Where(x => x.TenantId == scope.TenantId && x.EnvironmentId == scope.EnvironmentId &&
            (afterId == null || string.Compare(x.Id, afterId) > 0)).OrderBy(x => x.Id).Take(limit)
            .Select(x => new SlackSocketDiscardCleanupCandidate(x.Id, x.Revision)).ToListAsync(cancellationToken);
    }

    public async Task<bool> CleanupAsync(string receiptId, long revision, DateTimeOffset now, string authority, CancellationToken cancellationToken = default)
    {
        if (!Bounded(receiptId, 256) || !Bounded(authority, AdmissionLimits.AuthorityBytes))
        {
            throw new ArgumentException("socket_receipt_bounded_reference_required");
        }
        await using var admission = await CreateAsync(cancellationToken);
        await using var transaction = await admission.Database.BeginTransactionAsync(cancellationToken);
        await using var receipts = await transactions.EnlistAsync(admission, cancellationToken);
        var subscriptionId = await receipts.Receipts.Where(x => x.Id == receiptId).Select(x => x.SubscriptionId).SingleOrDefaultAsync(cancellationToken);
        if (subscriptionId == null)
        {
            return false;
        }
        await transactionLock.AcquireAsync(admission, subscriptionId, cancellationToken);
        var receipt = await receipts.Receipts.SingleOrDefaultAsync(x => x.Id == receiptId, cancellationToken);
        if (receipt == null || receipt.Revision != revision)
        {
            return false;
        }
        DemandScope(receipt);
        var subscription = await admission.Subscriptions.SingleAsync(x => x.Id == subscriptionId, cancellationToken);
        DemandScope(admission, subscription);
        var policy = JsonSerializer.Deserialize<AdmissionSubscriptionConfiguration>(receipt.ConfigurationJson)!.Policy;
        policy.Validate();
        now = now.ToUniversalTime();
        if (authority != policy.CleanupAuthority || now < receipt.DecisionAt || now - receipt.DecisionAt < policy.IdentityHorizon)
        {
            return false;
        }
        if (subscription.RetainedRecords <= subscription.ActiveReservations)
        {
            throw new InvalidOperationException("socket_receipt_capacity_invariant_failed");
        }
        receipts.Receipts.Remove(receipt);
        subscription.RetainedRecords--;
        subscription.Revision = checked(subscription.Revision + 1);
        await receipts.SaveChangesAsync(cancellationToken);
        await admission.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<AdmissionElsaDbContext> CreateAsync(CancellationToken cancellationToken)
    {
        scope.Validate();
        var admission = await admissionFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            SlackSocketReceiptTransactions.DemandProvider(admission);
            return admission;
        }
        catch
        {
            await admission.DisposeAsync();
            throw;
        }
    }

    private void DemandScope(AdmissionElsaDbContext db, AdmissionSubscription subscription)
    {
        if ((string?)db.Entry(subscription).Property("TenantId").CurrentValue != scope.TenantId ||
            (string?)db.Entry(subscription).Property("EnvironmentId").CurrentValue != scope.EnvironmentId)
        {
            throw new InvalidOperationException("socket_receipt_trusted_scope_conflict");
        }
    }

    private void DemandScope(SlackSocketDiscardReceipt receipt)
    {
        if (receipt.TenantId != scope.TenantId || receipt.EnvironmentId != scope.EnvironmentId)
        {
            throw new InvalidOperationException("socket_receipt_trusted_scope_conflict");
        }
    }

    private static DateTimeOffset RoundUp(DateTimeOffset value)
    {
        var ticks = value.UtcTicks;
        var remainder = ticks % TimeSpan.TicksPerMicrosecond;
        return new(remainder == 0 ? ticks : checked(ticks + TimeSpan.TicksPerMicrosecond - remainder), TimeSpan.Zero);
    }

    private static bool Bounded(string? value, int bytes) => !string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) <= bytes;
    private static bool Fingerprint(string? value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
    private static SlackSocketDiscardResult Result(SlackSocketDiscardOutcome outcome) => new(outcome, null, null);
    private static SlackSocketDiscardResult Rejected(AdmissionRejectedEventDisposition disposition) => Result(disposition == AdmissionRejectedEventDisposition.Quarantine ? SlackSocketDiscardOutcome.Quarantined : SlackSocketDiscardOutcome.Rejected);
}
