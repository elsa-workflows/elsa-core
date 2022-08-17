using Elsa.ProtoActor.Common.Options;
using Proto;

namespace Elsa.ProtoActor.Common;

public static class DependencyInjectionExtensions
{
    public static ActorSystemConfig WithDeveloperLogging(this ActorSystemConfig actorSystemConfig, Action<DeveloperLoggingOptions>? developerLoggingOption = null)
    {
        var options = new DeveloperLoggingOptions();
        developerLoggingOption?.Invoke(options);
        
        return actorSystemConfig.WithDeveloperSupervisionLogging(true)
            .WithDeveloperReceiveLogging(options.ReceiveLoggingTimeSpan)
            .WithDeadLetterThrottleCount(options.DeadLetterThrottleCount)
            .WithDeadLetterThrottleInterval(options.DeadLetterThrottleInterval)
            .WithDeadLetterRequestLogging(true);
    }

    public static ActorSystemConfig ConfigureActorSystemConfig(this ActorSystemConfig systemConfig,
        Action<ActorSystemConfig> callback)
    {
        callback(systemConfig);
        return systemConfig;
    }
}