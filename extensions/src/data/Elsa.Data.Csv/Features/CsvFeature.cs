using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Data.Csv.Features;

/// <summary>
/// Setup email features.
/// </summary>
public class CsvFeature : FeatureBase
{
    /// <inheritdoc />
    public CsvFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<CsvFeature>();
    }
}