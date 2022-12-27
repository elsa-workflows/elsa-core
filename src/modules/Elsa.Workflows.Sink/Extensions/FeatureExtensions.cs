using System;
using Elsa.Workflows.Sink.Features;

namespace Microsoft.Extensions.DependencyInjection;

public static class FeatureExtensions
{
    public static WorkflowSinkFeature UseInMemoryDatabase(this WorkflowSinkFeature feature, Action<InMemoryWorkflowSinkPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    public static WorkflowSinkFeature UseMassTransitServiceBus(this WorkflowSinkFeature feature, Action<MassTransitSinkTransportFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}