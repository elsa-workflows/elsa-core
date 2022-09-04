using System;
using Elsa.ProtoActor.Features;
using Elsa.ProtoActor.Options;
using Elsa.Workflows.Runtime.Features;
using Proto;

namespace Elsa.ProtoActor.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowRuntimeFeature UseProtoActor(this WorkflowRuntimeFeature feature, Action<ProtoActorFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
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
