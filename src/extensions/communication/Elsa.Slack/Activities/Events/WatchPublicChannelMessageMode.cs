namespace Elsa.Slack.Activities.Events;

/// <summary>Selects the existing token path or explicitly opted-in admitted message data.</summary>
public enum WatchPublicChannelMessageMode
{
    LegacyToken = 0,
    AdmittedPublicMessage = 1
}
