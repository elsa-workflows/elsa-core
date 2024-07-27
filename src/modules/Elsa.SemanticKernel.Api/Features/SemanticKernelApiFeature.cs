using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.SemanticKernel.Features;
using JetBrains.Annotations;

namespace Elsa.SemanticKernel.Api.Features;

/// A feature that installs API endpoints to interact with skilled agents.
[DependsOn(typeof(SemanticKernelFeature))]
[UsedImplicitly]
public class SemanticKernelApiFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<SemanticKernelApiFeature>();
    }
}