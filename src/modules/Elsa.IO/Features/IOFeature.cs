using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Features;

/// <summary>
/// A feature that installs IO services for resolving various content types to streams.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
public class IOFeature : FeatureBase
{
    /// <inheritdoc />
    public override void Configure()
    {
        Services.AddScoped<IContentResolver, ContentResolver>();
    }
}