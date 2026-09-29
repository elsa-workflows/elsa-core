namespace Elsa.Telnyx;

public static class WebhookEventTypes
{
    public const string CallAnswered = "call.answered";
    public const string CallBridged = "call.bridged";
    public const string CallDtmfReceived = "call.dtmf.received";
    public const string CallGatherEnded = "call.gather.ended";
    public const string CallHangup = "call.hangup";
    public const string CallInitiated = "call.initiated";
    public const string CallMachineGreetingEnded = "call.machine.greeting.ended";
    public const string CallMachinePremiumGreetingEnded = "call.machine.premium.greeting.ended";
    public const string CallMachineDetectionEnded = "call.machine.detection.ended";
    public const string CallMachinePremiumDetectionEnded = "call.machine.premium.detection.ended";
    public const string CallPlaybackStarted = "call.playback.started";
    public const string CallPlaybackEnded = "call.playback.ended";
    public const string CallRecordingSaved = "call.recording.saved";
    public const string CallSpeakStarted = "call.speak.started";
    public const string CallSpeakEnded = "call.speak.ended";
}