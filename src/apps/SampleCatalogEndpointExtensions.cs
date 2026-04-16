using Nuplane.Abstractions;
using Nuplane.Loading;

namespace Nuplane.Sample.AspNetCore.Catalog;

internal static class SampleCatalogEndpointExtensions
{
    public static IEndpointRouteBuilder MapSampleCatalog(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/catalog/packages", async (IActivePackageCatalog catalog, CancellationToken cancellationToken) =>
            Results.Ok(await catalog.GetActivePackagesAsync(cancellationToken)));

        endpoints.MapGet("/catalog/load-state", async (IPackageLoadStateCatalog loadStateCatalog, CancellationToken cancellationToken) =>
            Results.Ok(await loadStateCatalog.GetLoadStateAsync(cancellationToken)));

        endpoints.MapGet("/catalog/assemblies", async (IPackageAssemblyCatalog packageAssemblyCatalog, CancellationToken cancellationToken) =>
        {
            var assemblies = (await packageAssemblyCatalog.GetAssembliesAsync(cancellationToken))
                .Select(AssemblyCatalogResponses.FromEntry)
                .ToArray();

            return Results.Ok(assemblies);
        });

        endpoints.MapGet("/catalog/assemblies/{packageId}", async (string packageId, IPackageAssemblyCatalog packageAssemblyCatalog, CancellationToken cancellationToken) =>
        {
            var package = await packageAssemblyCatalog.GetAssembliesAsync(packageId, cancellationToken);
            return package is null
                ? Results.NotFound(AssemblyCatalogResponses.MissingPackage(packageId))
                : Results.Ok(AssemblyCatalogResponses.FromEntry(package));
        });

        endpoints.MapGet("/catalog/plugins", async (PluginCatalog pluginCatalog, CancellationToken cancellationToken) =>
            Results.Ok(await pluginCatalog.DiscoverAsync(cancellationToken)));

        return endpoints;
    }
}


