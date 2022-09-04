using System;

namespace Elsa.ProtoActor.Options;

public class DeveloperLoggingOptions
{
    public TimeSpan ReceiveLoggingTimeSpan { get; set; } = TimeSpan.FromHours(1);
    public int DeadLetterThrottleCount { get; set; } = 3;
    public TimeSpan DeadLetterThrottleInterval { get; set; } = TimeSpan.FromSeconds(10000);
}