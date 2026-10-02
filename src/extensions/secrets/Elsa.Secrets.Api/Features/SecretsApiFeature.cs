using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Management.Features;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[DependsOn(typeof(SecretManagementFeature))]
[UsedImplicitly]
public class SecretsApiFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<SecretsApiFeature>();
    }
}