using Elsa.Workflows;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode.Events;

internal sealed class AdmittedSlackPublicChannelMessageSource(SlackSocketModeConfiguration configuration,
    IAdmissionExecutionDataReader reader) : ISlackPublicChannelMessageSource
{
    public async ValueTask<SlackPublicChannelMessage> ReadAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
    {
        var workflow = context.WorkflowExecutionContext;
        var data = await reader.ReadConsumedEventAsync(workflow, cancellationToken);
        // These normal workflow inputs are only a consistency check. They cannot establish the
        // exact private consumed-owner binding which the Admission reader required independently.
        if (!workflow.Input.TryGetValue("Event", out var eventValue) || eventValue is not string input || input != data.Payload ||
            !workflow.Input.TryGetValue("ProviderEventId", out var idValue) || idValue is not string eventId || eventId != data.ProviderEventId ||
            !workflow.Input.TryGetValue("ChannelId", out var channelValue) || channelValue is not string channel || channel != data.Configuration.ChannelId ||
            data.Configuration.TenantId != configuration.TenantId || data.Configuration.EnvironmentId != configuration.EnvironmentId ||
            data.Configuration.InstallationId != configuration.InstallationId || data.Configuration.ChannelId != configuration.ChannelId)
        {
            throw new InvalidOperationException("The admitted public message does not match the trusted listener binding.");
        }
        var subscriptions = configuration.Subscriptions.Where(x => x.Configuration.Id == data.Configuration.Id).ToArray();
        if (subscriptions.Length != 1 || subscriptions[0].Configuration.ConfigurationFingerprint != data.Configuration.ConfigurationFingerprint ||
            subscriptions[0].ActivationEpoch != data.ActivationEpoch)
        {
            throw new InvalidOperationException("The admitted public message does not match a pinned subscription.");
        }
        var message = SlackSocketEventPayload.DeserializeValidatedHuman(data.Payload);
        if (message.BindingFingerprint != configuration.BindingFingerprint || message.AppId != configuration.ExpectedAppId ||
            message.TeamId != configuration.ExpectedTeamId || message.EnterpriseId != configuration.ExpectedEnterpriseId ||
            message.SelfUserId != configuration.SelfUserId || message.ChannelId != configuration.ChannelId ||
            message.ProviderEventId != data.ProviderEventId || message.OccurredAt != data.OccurredAt)
        {
            throw new InvalidOperationException("The normalized public message does not match its durable event binding.");
        }
        var user = message.GetRequiredString("user");
        if (user == configuration.SelfUserId)
        {
            throw new InvalidOperationException("Self messages cannot enter the admitted human message path.");
        }
        return new(message.ChannelId, message.GetRequiredString("text"), message.GetRequiredString("ts"), user,
            message.GetOptionalString("thread_ts"), configuration.SelfUserId);
    }
}
