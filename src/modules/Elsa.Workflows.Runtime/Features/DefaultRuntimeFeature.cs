using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Workflows.Runtime.Features;

public class DefaultRuntimeFeature : FeatureBase
{
    public DefaultRuntimeFeature(IModule module) : base(module)
    {
    }
}