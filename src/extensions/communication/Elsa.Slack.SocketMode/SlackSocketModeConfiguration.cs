using System.Text;
using System.Text.Json;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode;

/// <summary>A trusted subscription and its captured activation epoch, independent of socket generations.</summary>
public sealed record SlackSocketSubscription(AdmissionSubscriptionConfiguration Configuration, long ActivationEpoch);

/// <summary>Explicit finite limits. The adapter supplies no production retention or transport defaults.</summary>
public sealed record SlackSocketModeLimits(
    int MaximumEnvelopeBytes,
    int MaximumFragments,
    TimeSpan MaximumAssemblyTime,
    int MaximumJsonDepth,
    int MaximumPendingEnvelopes,
    int MaximumPendingAcknowledgements,
    int MaximumAdmissionConcurrency,
    int MaximumFanOut,
    int MaximumReconnectAttempts,
    TimeSpan ReconnectDelay,
    TimeSpan MaximumReconnectDelay,
    TimeSpan OperationTimeout,
    TimeSpan DrainTimeout)
{
    internal void Validate()
    {
        if (MaximumEnvelopeBytes is <= 0 or > AdmissionLimits.PayloadBytes || MaximumFragments <= 0 ||
            MaximumJsonDepth is <= 0 or > 64 || MaximumPendingEnvelopes <= 0 ||
            MaximumPendingAcknowledgements <= 0 || MaximumAdmissionConcurrency <= 0 || MaximumFanOut <= 0 ||
            MaximumAdmissionConcurrency > MaximumPendingEnvelopes || MaximumReconnectAttempts <= 0 ||
            !FiniteTimeout(MaximumAssemblyTime) || !FiniteTimeout(ReconnectDelay) ||
            !FiniteTimeout(MaximumReconnectDelay) || MaximumReconnectDelay < ReconnectDelay ||
            !FiniteTimeout(OperationTimeout) || !FiniteTimeout(DrainTimeout))
        {
            throw new ArgumentException("Socket Mode requires explicit finite transport limits.");
        }
    }

    private static bool FiniteTimeout(TimeSpan value) => value > TimeSpan.Zero && value.TotalMilliseconds <= int.MaxValue;
}

/// <summary>Immutable server-selected route. Provider payloads cannot select connections, workflows or subscriptions.</summary>
public sealed class SlackSocketModeConfiguration
{
    public SlackSocketModeConfiguration(string tenantId, string environmentId, string installationId, string connectionId,
        string expectedAppId, string? expectedTeamId, string? expectedEnterpriseId, string selfUserId, string channelId,
        IReadOnlyList<SlackSocketSubscription> subscriptions, SlackSocketModeLimits limits)
    {
        foreach (var value in new[] { tenantId, environmentId, installationId, connectionId, expectedAppId, selfUserId, channelId })
        {
            ValidateIdentifier(value);
        }
        if (expectedTeamId is not null)
        {
            ValidateIdentifier(expectedTeamId);
        }
        if (expectedEnterpriseId is not null)
        {
            ValidateIdentifier(expectedEnterpriseId);
        }
        if (expectedTeamId is null && expectedEnterpriseId is null)
        {
            throw new ArgumentException("A workspace or enterprise installation identity is required.");
        }
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();
        ArgumentNullException.ThrowIfNull(subscriptions);
        var copy = subscriptions.ToArray();
        if (copy.Length == 0 || copy.Length > limits.MaximumFanOut ||
            copy.Any(x => x is null || x.Configuration is null) ||
            copy.Select(x => x.Configuration.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException("A bounded, unique and complete subscription set is required.");
        }
        foreach (var subscription in copy)
        {
            var configuration = subscription.Configuration;
            configuration.Validate();
            if (subscription.ActivationEpoch <= 0 || configuration.TenantId != tenantId ||
                configuration.EnvironmentId != environmentId || configuration.InstallationId != installationId ||
                configuration.ChannelId != channelId)
            {
                throw new ArgumentException("Every subscription must belong to the immutable listener route and epoch.");
            }
        }

        TenantId = tenantId;
        EnvironmentId = environmentId;
        InstallationId = installationId;
        ConnectionId = connectionId;
        ExpectedAppId = expectedAppId;
        ExpectedTeamId = expectedTeamId;
        ExpectedEnterpriseId = expectedEnterpriseId;
        SelfUserId = selfUserId;
        ChannelId = channelId;
        Subscriptions = Array.AsReadOnly(copy.OrderBy(x => x.Configuration.Id, StringComparer.Ordinal).ToArray());
        Limits = limits;
        BindingFingerprint = AdmissionHash.Compute(JsonSerializer.Serialize(new
        {
            Version = 1, TenantId, EnvironmentId, InstallationId, ConnectionId, ExpectedAppId, ExpectedTeamId,
            ExpectedEnterpriseId, SelfUserId, ChannelId,
            Subscriptions = Subscriptions.Select(x => new { x.Configuration.ConfigurationFingerprint, x.ActivationEpoch })
        }));
    }

    public string TenantId { get; }
    public string EnvironmentId { get; }
    public string InstallationId { get; }
    public string ConnectionId { get; }
    public string ExpectedAppId { get; }
    public string? ExpectedTeamId { get; }
    public string? ExpectedEnterpriseId { get; }
    public string SelfUserId { get; }
    public string ChannelId { get; }
    public IReadOnlyList<SlackSocketSubscription> Subscriptions { get; }
    public SlackSocketModeLimits Limits { get; }
    public string BindingFingerprint { get; }

    internal static void ValidateIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > 256 || value.Any(char.IsControl))
        {
            throw new ArgumentException("Socket Mode identifiers must be nonempty and bounded.");
        }
    }
}
