using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class MemoryCacheFeature : FeatureBase
{
    public MemoryCacheFeature(IModule module) : base(module)
    {
    }

    public override void Configure() => Services.AddMemoryCache();
}