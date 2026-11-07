using Elsa.Testing.Shared;

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
        var versions = RestoredPackageVersionReader.Read(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version =>
            Assert.True(
                version > LastAffected,
                $"{PackageId} {version} is still in GHSA-2m69-gcr7-jv3q (<= 2.1.11)."));
    }
}
