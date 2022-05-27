using Elsa.AspNetCore.Conventions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AspNetCore.Features;

public class MvcFeature : FeatureBase
{
    public MvcFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Services.AddMvc(mvc => mvc.Conventions.Add(new ApiEndpointAttributeConvention()));
    }
}