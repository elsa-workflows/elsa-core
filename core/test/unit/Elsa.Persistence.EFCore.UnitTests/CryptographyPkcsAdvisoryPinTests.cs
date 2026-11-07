using System.Text.Json;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// SqlClient and Oracle pull System.Security.Cryptography.Pkcs into this graph.
/// GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx: 10.0.0-10.0.12, 9.0.0-9.0.20, and 8.0.0-8.0.1
/// are affected; 10.0.13 is the first patched 10.x version.
/// </summary>
public class CryptographyPkcsAdvisoryPinTests
{
    private const string PackageId = "System.Security.Cryptography.Pkcs";
    private static readonly Version FirstPatchedNet10 = new(10, 0, 13);
    private static readonly Version LastAffectedNet8 = new(8, 0, 1);
    private static readonly Version LastAffectedNet9 = new(9, 0, 20);

    [Fact]
    public void RestoreGraph_PinsPkcsOutsideAdvisoryRanges()
    {
        var versions = RestoredPackageVersions(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version =>
            Assert.True(
                IsPatched(version),
                $"{PackageId} {version} is still in GHSA-2fmv-rv2c-59x7 / GHSA-846f-f764-76mx."));
    }

    private static bool IsPatched(Version version) =>
        version >= FirstPatchedNet10
        || (version.Major == 9 && version > LastAffectedNet9)
        || (version.Major == 8 && version > LastAffectedNet8);

    private static IReadOnlyList<Version> RestoredPackageVersions(string packageId)
    {
        var libraries = JsonDocument.Parse(File.ReadAllText(FindAssetsPath())).RootElement.GetProperty("libraries");
        return libraries.EnumerateObject()
            .Where(property => property.Name.StartsWith(packageId + "/", StringComparison.OrdinalIgnoreCase))
            .Select(property => Version.Parse(property.Name[(packageId.Length + 1)..].Split('-', '+')[0]))
            .ToList();
    }

    private static string FindAssetsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var assets = Path.Combine(directory.FullName, "obj", "project.assets.json");
            if (File.Exists(assets))
                return assets;
        }

        throw new FileNotFoundException("project.assets.json", AppContext.BaseDirectory);
    }
}
