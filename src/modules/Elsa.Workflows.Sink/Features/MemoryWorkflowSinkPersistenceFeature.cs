using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sink.Features;

[DependsOn(typeof(WorkflowSinkFeature))]
public class MemoryWorkflowSinkPersistenceFeature : FeatureBase
{
    public MemoryWorkflowSinkPersistenceFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        Module.Configure<WorkflowSinkFeature>().WorkflowSinkClient = default;
    }

    public override void Apply()
    {
        Services.AddSingleton<IWorkflowSinkClient, MemoryWorkflowSinkClient>();
    }
}