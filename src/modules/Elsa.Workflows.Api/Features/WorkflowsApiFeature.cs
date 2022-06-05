using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Api.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

public class WorkflowsApiFeature : FeatureBase
{
    public WorkflowsApiFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Services.AddSingleton<VariableDefinitionMapper>();
    }
}