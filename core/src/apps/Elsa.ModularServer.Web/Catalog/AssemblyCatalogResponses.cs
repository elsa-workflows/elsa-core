using Nuplane.Loading;

namespace Elsa.ModularServer.Web.Catalog;

internal static class AssemblyCatalogResponses
{
    public static AssemblyCatalogPackageResponse FromEntry(PackageAssemblies package) =>
        new(
            package.PackageId,
            package.Version,
            package.Assemblies
                .Select(static assembly => new AssemblyDescriptorResponse(
                    assembly.GetName().Name ?? assembly.FullName ?? "<unknown>"))
                .OrderBy(static assembly => assembly.Name, StringComparer.Ordinal)
                .ToArray(),
            package.AssemblyReferences
                .Select(static candidate => new AssemblyReferenceResponse(
                    candidate.AssemblyFileName,
                    candidate.TargetFrameworkMoniker,
                    candidate.Kind))
                .OrderBy(static reference => reference.AssemblyFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray());

    public static AssemblyCatalogNotFoundResponse MissingPackage(string packageId) =>
        new(packageId, null, "package-not-active-or-not-loaded");

    public static AssemblyCatalogNotFoundResponse MissingPackageVersion(string packageId, string version) =>
        new(packageId, version, "package-not-active-or-not-loaded");
}

internal sealed record AssemblyCatalogPackageResponse(
    string PackageId,
    string Version,
    IReadOnlyList<AssemblyDescriptorResponse> LoadedAssemblies,
    IReadOnlyList<AssemblyReferenceResponse> SelectedAssemblyReferences);

internal sealed record AssemblyDescriptorResponse(string Name);

internal sealed record AssemblyReferenceResponse(
    string AssemblyFileName,
    string? TargetFrameworkMoniker,
    string Kind);

internal sealed record AssemblyCatalogNotFoundResponse(
    string PackageId,
    string? Version,
    string Reason);

