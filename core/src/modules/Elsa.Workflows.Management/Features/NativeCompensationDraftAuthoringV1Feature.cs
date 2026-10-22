using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Advertises support for effect-free native compensation draft authoring.
/// </summary>
[UsedImplicitly]
public sealed class NativeCompensationDraftAuthoringV1Feature(IModule module) : FeatureBase(module)
{
}
