using Elsa.Testing.Shared;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

/// <summary>
/// FluentMigrator.Runner pulls System.Security.Cryptography.Pkcs into StructuredLogs Relational/Sqlite.
/// GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx: 8.0.0-8.0.1, 9.0.0-9.0.20, and 10.0.0-10.0.12
/// are affected. Central PackageVersion entries do not pin transitives; the Relational graph-root
/// PackageReference must resolve 8.0.2 / 9.0.21 / 10.0.13, and Sqlite inherits that pin.
/// </summary>
public class StructuredLogsPkcsAdvisoryPinTests
{
    private const string PackageId = "System.Security.Cryptography.Pkcs";

    private static readonly string[] StructuredLogsProjects =
    [
        Path.Combine("core", "src", "modules", "Elsa.Diagnostics.StructuredLogs.Persistence.Relational"),
        Path.Combine("core", "src", "modules", "Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite")
    ];

    [Fact]
    public void RestoreGraph_PinsPkcsToTfmPatchedVersion()
    {
        var versions = RestoredPackageVersionReader.Read(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version => Assert.Equal(ExpectedPatchedVersion, version));
    }

    [Fact]
    public void PackableStructuredLogsProjects_ResolvePatchedPkcsForThisTfm()
    {
        var root = FindRepoRoot();
        var tfm = CurrentTfm;
        foreach (var relative in StructuredLogsProjects)
        {
            var versions = RestoredPackageVersionReader.ReadForTarget(PackageId, tfm, Path.Combine(root, relative));
            Assert.True(versions.Count > 0, $"{relative} restored no {PackageId} for {tfm}.");
            Assert.All(versions, version => Assert.Equal(ExpectedPatchedVersion, version));
        }
    }

    private static string CurrentTfm
    {
        get
        {
#if NET8_0
            return "net8.0";
#elif NET9_0
            return "net9.0";
#else
            return "net10.0";
#endif
        }
    }

    private static Version ExpectedPatchedVersion
    {
        get
        {
#if NET8_0
            return new Version(8, 0, 2);
#elif NET9_0
            return new Version(9, 0, 21);
#else
            return new Version(10, 0, 13);
#endif
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }
}
