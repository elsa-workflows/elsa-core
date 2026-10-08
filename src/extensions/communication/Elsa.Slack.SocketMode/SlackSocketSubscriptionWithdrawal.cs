using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode;

// The parent host owns tenant scope, deadline, intake retirement and actual callback/owner drain.
// This helper establishes only the complete captured set's definite inactive ledger boundary.
internal sealed class SlackSocketSubscriptionWithdrawal(SlackSocketModeConfiguration configuration, IAdmissionStore store)
{
    internal async Task<bool> WithdrawAsync(CancellationToken cancellationToken)
    {
        foreach (var captured in configuration.Subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await store.FindSubscriptionAsync(captured.Configuration.Id, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current == null || !Matches(current, captured) || current.ReconciliationCode != null)
            {
                return false;
            }
            if (!current.Active)
            {
                continue;
            }
            // Exactly one CAS per member. A stale revision is a definite incomplete result;
            // an unknown commit escapes, with no readback/retry or cross-epoch withdrawal.
            var withdrawn = await store.WithdrawAsync(current.Id, current.Revision, false, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (withdrawn == null || !Matches(withdrawn, captured) || withdrawn.Active || withdrawn.ReconciliationCode != null)
            {
                return false;
            }
        }
        return true;
    }

    private bool Matches(AdmissionSubscription subscription, SlackSocketSubscription captured)
    {
        if (subscription.Id != captured.Configuration.Id ||
            subscription.ActivationEpoch != captured.ActivationEpoch ||
            subscription.ConfigurationFingerprint != captured.Configuration.ConfigurationFingerprint)
        {
            return false;
        }
        var actual = subscription.Configuration;
        return actual.Id == captured.Configuration.Id && actual.TenantId == configuration.TenantId &&
            actual.EnvironmentId == configuration.EnvironmentId && actual.InstallationId == configuration.InstallationId &&
            actual.ChannelId == configuration.ChannelId && actual.ConfigurationFingerprint == subscription.ConfigurationFingerprint;
    }
}
