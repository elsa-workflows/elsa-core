using Elsa.ExternalAuthentication.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Features;

/// <summary>
/// Configures the <see cref="ExternalAuthenticationFeature"/> feature with Entity Framework Core persistence.
/// </summary>
[DependsOn(typeof(ExternalAuthenticationFeature))]
public class EFCoreExternalAuthenticationPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreExternalAuthenticationPersistenceFeature, ExternalAuthenticationElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        Services.AddExternalAuthenticationEntityFrameworkCore();
    }
}
