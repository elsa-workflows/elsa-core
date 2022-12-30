using System;
using Elsa.Workflows.Sinks.Features;

namespace Microsoft.Extensions.DependencyInjection;

public static class FeatureExtensions
{
    public static WorkflowSinkFeature UseMassTransitServiceBus(this WorkflowSinkFeature feature, Action<MassTransitSinkTransportFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}