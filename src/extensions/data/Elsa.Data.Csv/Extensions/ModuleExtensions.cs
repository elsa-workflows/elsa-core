using Elsa.Data.Csv.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides methods to install and configure email related features.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="EmailFeature"/> feature to the system.
    /// </summary>
    public static IModule UseCsv(this IModule configuration, Action<CsvFeature>? configure = null)
    {
        configuration.Configure(configure);
        return configuration;
    }
}