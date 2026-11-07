using System.Text.Json;

namespace Elsa.Scheduling.Hangfire.UnitTests;

/// <summary>
/// Hangfire.Storage.SQLite 0.4.2 pulls sqlite-net-pcl 1.8.116 → SQLitePCLRaw.bundle_green 2.0.4 →
/// SQLitePCLRaw.lib.e_sqlite3 2.0.4 (native sqlite 3.49.1 at 2.1.11). GHSA-2m69-gcr7-jv3q last
/// affected version is 2.1.11.
/// </summary>
public class HangfireSqliteNativeLibraryPinTests
{
    private const string PackageId = "SQLitePCLRaw.lib.e_sqlite3";
    private static readonly Version LastAffected = new(2, 1, 11);

    [Fact]
    public void RestoreGraph_PinsSqlitePclRawLibAboveAdvisoryRange()
    {
        var versions = RestoredPackageVersions(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version =>
            Assert.True(
                version > LastAffected,
                $"{PackageId} {version} is still in GHSA-2m69-gcr7-jv3q (<= 2.1.11)."));
    }

    private static IReadOnlyList<Version> RestoredPackageVersions(string packageId)
    {
        var libraries = JsonDocument.Parse(File.ReadAllText(FindAssetsPath())).RootElement.GetProperty("libraries");
        return libraries.EnumerateObject()
            .Where(property => property.Name.StartsWith(packageId + "/", StringComparison.OrdinalIgnoreCase))
            .Select(property => Version.Parse(property.Name[(packageId.Length + 1)..]))
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
