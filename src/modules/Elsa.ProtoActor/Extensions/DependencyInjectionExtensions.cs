using System;
using Elsa.ProtoActor.Configuration;
using Elsa.Workflows.Runtime.Configuration;

namespace Elsa.ProtoActor.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowRuntimeConfigurator UseProtoActor(this WorkflowRuntimeConfigurator configurator, Action<ProtoActorConfigurator>? configure = default)
    {
        configurator.ServiceConfiguration.Configure(configure);
        return configurator;
    }
}