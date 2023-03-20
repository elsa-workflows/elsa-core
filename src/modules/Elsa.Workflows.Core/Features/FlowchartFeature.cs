using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Serialization;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Features;

public class FlowchartFeature : FeatureBase
{
    public FlowchartFeature(IModule module) : base(module)
    {
    }
    
    public override void Apply()
    {
        Services.AddSingleton<ISerializationOptionsConfigurator, FlowchartSerializationOptionConfigurator>();
    }
}