using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Handlers;
using Elsa.Workflows.Core.Activities.Flowchart.Implementations;
using Elsa.Workflows.Core.Activities.Flowchart.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Features;

public class FlowchartFeature : FeatureBase
{
    public FlowchartFeature(IModule module) : base(module)
    {
    }
    
    public override void Apply()
    {
        Services
            .AddSingleton<ITransposeHandlerRegistry, TransposeHandlerRegistry>()
            .AddSingleton<ITransposeHandler, DefaultTransposeHandler>()
            .AddSingleton<IActivityNodeDescriber, ActivityNodeDescriber>()
            .AddSingleton<ISerializationOptionsConfigurator, FlowchartSerializationOptionConfigurator>()
            ;
    }
}