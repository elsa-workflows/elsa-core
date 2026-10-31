namespace Elsa.Telnyx;

public static class WebhookActivityTypeNames
{
    public const string CallBridged = $"{Constants.Namespace}.{nameof(CallBridged)}";
    public const string CallDtmfReceived = $"{Constants.Namespace}.{nameof(CallDtmfReceived)}";
    public const string CallGatherEnded = $"{Constants.Namespace}.{nameof(CallGatherEnded)}";
    public const string CallHangup = $"{Constants.Namespace}.{nameof(CallHangup)}";
    public const string CallInitiated = $"{Constants.Namespace}.{nameof(CallInitiated)}";
    public const string CallMachineGreetingEnded = $"{Constants.Namespace}.{nameof(CallMachineGreetingEnded)}";
    public const string CallMachinePremiumGreetingEnded = $"{Constants.Namespace}.{nameof(CallMachinePremiumGreetingEnded)}";
    public const string CallMachineDetectionEnded = $"{Constants.Namespace}.{nameof(CallMachineDetectionEnded)}";
    public const string CallMachinePremiumDetectionEnded = $"{Constants.Namespace}.{nameof(CallMachinePremiumDetectionEnded)}";
    public const string CallPlaybackStarted = $"{Constants.Namespace}.{nameof(CallPlaybackStarted)}";
    public const string CallPlaybackEnded = $"{Constants.Namespace}.{nameof(CallPlaybackEnded)}";
    public const string CallRecordingSaved = $"{Constants.Namespace}.{nameof(CallRecordingSaved)}";
    public const string CallSpeakStarted = $"{Constants.Namespace}.{nameof(CallSpeakStarted)}";
    public const string CallSpeakEnded = $"{Constants.Namespace}.{nameof(CallSpeakEnded)}";
}